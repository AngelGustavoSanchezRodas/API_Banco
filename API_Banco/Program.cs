using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services;
using API_Banco.Infrastructure.Integrations;
using API_Banco.Infrastructure.Persistence;
using API_Banco.Infrastructure.Repositories;
using API_Banco.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace API_Banco
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Configurar CORS (Obligatorio para que Next.js o cualquier front no sea bloqueado)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("NextJsPolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // 2. Extraer la cadena de conexión
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 3. Registrar el DbContext con Pomelo MySQL
            builder.Services.AddDbContext<BancoDbContext>(options =>
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 32)))
            );

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // [NUEVO] Registrar el servicio base de Autenticación para que [Authorize] funcione
            builder.Services.AddAuthentication();

            // 4. Inyección de la Capa de Aplicación (Servicios)
            builder.Services.AddScoped<ICuentahabienteServicio, CuentahabienteServicio>();
            builder.Services.AddScoped<IOperacionesFinancierasServicio, OperacionesFinancierasServicio>();
            builder.Services.AddScoped<IPagoServiciosServicio, PagoServiciosServicio>();
            builder.Services.AddScoped<IBitacoraServicio, BitacoraServicio>();
            builder.Services.AddSingleton<IProveedorFecha, ProveedorFechaSistema>();

            // 5. Inyección de la Capa de Infraestructura (Repositorios)
            builder.Services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
            builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
            builder.Services.AddScoped<ICuentaRepositorio, CuentaRepositorio>();
            builder.Services.AddScoped<ITransaccionRepositorio, TransaccionRepositorio>();
            builder.Services.AddScoped<ITipoTransaccionRepositorio, TipoTransaccionRepositorio>();
            builder.Services.AddScoped<IEstadoRepositorio, EstadoRepositorio>();
            builder.Services.AddScoped<ITarjetaDebitoRepositorio, TarjetaDebitoRepositorio>();
            builder.Services.AddScoped<IRegistroPagoServicioRepositorio, RegistroPagoServicioRepositorio>();

            // 6. Inyección de Servicios Externos (Integración HTTP)
            builder.Services.AddHttpClient();

            builder.Services.AddHttpClient("UniversidadApi", client =>
            {
                var universidadUrl = builder.Configuration["Integraciones:UniversidadApiUrl"]
                    ?? throw new InvalidOperationException("Falta configurar Integraciones:UniversidadApiUrl.");
                client.BaseAddress = new Uri(universidadUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);

                var universidadApiKey = builder.Configuration["Integraciones:UniversidadApiKey"];
                if (!string.IsNullOrWhiteSpace(universidadApiKey))
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", universidadApiKey);
                }
            });

            builder.Services.AddHttpClient("EnergiaApi", client =>
            {
                var energiaUrl = builder.Configuration["Integraciones:EnergiaApiUrl"]
                    ?? throw new InvalidOperationException("Falta configurar Integraciones:EnergiaApiUrl.");
                client.BaseAddress = new Uri(energiaUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);

                var energiaApiKey = builder.Configuration["Integraciones:EnergiaApiKey"];
                if (!string.IsNullOrWhiteSpace(energiaApiKey))
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", energiaApiKey);
                }
            });

            var telefoniaUrl = builder.Configuration["Integraciones:TelefoniaApiUrl"]?.Trim();
            if (!string.IsNullOrWhiteSpace(telefoniaUrl)
                && !telefoniaUrl.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(telefoniaUrl, UriKind.Absolute, out _))
            {
                builder.Services.AddHttpClient("TelefoniaApi", client =>
                {
                    client.BaseAddress = new Uri(telefoniaUrl.TrimEnd('/') + "/");
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var telefoniaApiKey = builder.Configuration["Integraciones:TelefoniaApiKey"];
                    if (!string.IsNullOrWhiteSpace(telefoniaApiKey)
                        && !telefoniaApiKey.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase))
                    {
                        client.DefaultRequestHeaders.Add("X-Api-Key", telefoniaApiKey);
                    }
                });
            }

            builder.Services.AddScoped<GestorIntegracionServicios>();
            builder.Services.AddScoped<IValidadorIdentificadorServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<INotificacionEmpresaServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<IConsultaDeudaServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<INumeroCuentaGenerador, GeneradoresMock>();
            builder.Services.AddScoped<INumeroTarjetaGenerador, GeneradoresMock>();
            builder.Services.AddScoped<IConfiguracionDistribucionPagos, ConfiguracionPagosMock>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.MapScalarApiReference();
                app.MapOpenApi();
            }

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("UnhandledException");
                    logger.LogError(ex, "Excepción no controlada en {Path}", context.Request.Path);

                    if (!context.Response.HasStarted)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";

                        var payload = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Excepción no controlada en el banco.",
                            tipo = ex.GetType().FullName,
                            mensaje = ex.Message,
                            innerMensaje = ex.InnerException?.Message,
                            path = context.Request.Path.Value
                        });
                        await context.Response.WriteAsync(payload);
                    }
                }
            });

            app.UseHttpsRedirection();

 
            app.UseRouting();                   // 1. Sabe a dónde va la petición
            app.UseCors("NextJsPolicy");        // 2. Deja pasar la petición (Aplica la política de arriba)
            app.UseAuthentication();            // 3. Autenticación (Prepara para leer Tokens)
            app.UseAuthorization();             // 4. Autorización (Aplica los [Authorize] y roles)
            app.MapControllers();               // 5. Ejecuta el controlador
           

            app.Run();
        }
    }
}