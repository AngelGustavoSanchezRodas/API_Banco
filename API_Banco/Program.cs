using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services;
using API_Banco.Auth;
using API_Banco.Infrastructure.Integrations;
using API_Banco.Infrastructure.Persistence;
using API_Banco.Infrastructure.Repositories;
using API_Banco.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
// [NUEVO] Librerías necesarias para JWT
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API_Banco
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========================================================================
            // 1. CONFIGURACIÓN ESTRICTA Y DINÁMICA DE CORS
            // ========================================================================
            // `FrontendUrl` puede contener UNA o VARIAS URLs separadas por coma para
            // soportar distintos entornos (localhost, Azure Static Web Apps, etc.)
            // sin redeployar el backend. Ej.:
            //   FrontendUrl = "https://localhost:5173,https://icy-rock-xxx.azurestaticapps.net"
            var frontendRaw = builder.Configuration["FrontendUrl"]
                ?? throw new InvalidOperationException("🚨 ERROR CRÍTICO: La variable de entorno 'FrontendUrl' no está configurada.");

            // Helper local: parsea una cadena con orígenes separados por coma o salto
            // de línea, limpia espacios/slashes, descarta valores vacíos y rechaza los
            // marcadores tipo "REEMPLAZAR_*" usados como placeholder en config.
            static IEnumerable<string> ParsearOrigenes(string? raw) =>
                (raw ?? string.Empty)
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(u => u.TrimEnd('/'))
                    .Where(u => u.Length > 0 && !u.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase));

            // Extraemos las URLs de las APIs integradas desde appsettings.json
            var universidadUrl = builder.Configuration["Integraciones:UniversidadApiUrl"];
            var energiaUrl = builder.Configuration["Integraciones:EnergiaApiUrl"];
            var telefoniaUrl = builder.Configuration["Integraciones:TelefoniaApiUrl"];

            // Construimos la lista de orígenes permitidos sin duplicados y normalizada.
            var origenesPermitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var u in ParsearOrigenes(frontendRaw)) origenesPermitidos.Add(u);
            foreach (var u in ParsearOrigenes(universidadUrl)) origenesPermitidos.Add(u);
            foreach (var u in ParsearOrigenes(energiaUrl))     origenesPermitidos.Add(u);
            foreach (var u in ParsearOrigenes(telefoniaUrl))   origenesPermitidos.Add(u);

            if (origenesPermitidos.Count == 0)
                throw new InvalidOperationException("🚨 ERROR CRÍTICO: 'FrontendUrl' no contiene ningún origen válido.");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ProduccionCORS", policy =>
                {
                    // Inyectamos el arreglo completo de orígenes válidos (Front + Integraciones)
                    policy.WithOrigins(origenesPermitidos.ToArray())
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // Obligatorio si alguna de las apps envía tokens/cookies
                });
            });
            // ========================================================================

            // 2. Extraer la cadena de conexión
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 3. Registrar el DbContext con Pomelo MySQL
            builder.Services.AddDbContext<BancoDbContext>(options =>
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 32)))
            );

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // ========================================================================
            // CONFIGURACIÓN DE JWT
            // ========================================================================
            var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta Jwt:Key en appsettings.json o en Azure Environment Variables");
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogError($"[AuthError] Autenticación fallida: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogWarning($"[AuthError] Challenge lanzado: {context.ErrorDescription}");
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationOptions.Scheme,
                    _ => { });

            // Policy "PortalOSocioBancario": cualquiera de los dos esquemas
            // (JWT del cliente o API Key del socio) autoriza el acceso.
            // Se usa en endpoints de pago expuestos tanto al frontend como a las APIs externas.
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("PortalOSocioBancario", policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.AuthenticationSchemes.Add(ApiKeyAuthenticationOptions.Scheme);
                    policy.RequireAuthenticatedUser();
                });
            });
            // ========================================================================

            // 4. Inyección de la Capa de Aplicación (Servicios)
            builder.Services.AddScoped<ICuentahabienteServicio, CuentahabienteServicio>();
            builder.Services.AddScoped<IOperacionesFinancierasServicio, OperacionesFinancierasServicio>();
            builder.Services.AddScoped<IPagoServiciosServicio, PagoServiciosServicio>();
            builder.Services.AddScoped<IBitacoraServicio, BitacoraServicio>();
            builder.Services.AddScoped<IAdminMetricasServicio, AdminMetricasServicio>();
            builder.Services.AddSingleton<IProveedorFecha, ProveedorFechaSistema>();
            builder.Services.AddSingleton<IHasherCredenciales, HasherCredencialesBCrypt>();

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
                var url = builder.Configuration["Integraciones:UniversidadApiUrl"]
                    ?? throw new InvalidOperationException("Falta configurar Integraciones:UniversidadApiUrl.");
                client.BaseAddress = new Uri(url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);

                var apiKey = builder.Configuration["Integraciones:UniversidadApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                }
            });

            builder.Services.AddHttpClient("EnergiaApi", client =>
            {
                var url = builder.Configuration["Integraciones:EnergiaApiUrl"]
                    ?? throw new InvalidOperationException("Falta configurar Integraciones:EnergiaApiUrl.");
                client.BaseAddress = new Uri(url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);

                var apiKey = builder.Configuration["Integraciones:EnergiaApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                }
            });

            var telUrlOriginal = builder.Configuration["Integraciones:TelefoniaApiUrl"]?.Trim();
            if (!string.IsNullOrWhiteSpace(telUrlOriginal)
                && !telUrlOriginal.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(telUrlOriginal, UriKind.Absolute, out _))
            {
                builder.Services.AddHttpClient("TelefoniaApi", client =>
                {
                    var baseUrl = IntegracionApiUrlNormalizer.NormalizarBaseTelefonia(telUrlOriginal);
                    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var apiKey = builder.Configuration["Integraciones:TelefoniaApiKey"];
                    if (!string.IsNullOrWhiteSpace(apiKey)
                        && !apiKey.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase))
                    {
                        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                    }
                });
            }

            builder.Services.AddScoped<GestorIntegracionServicios>();
            builder.Services.AddScoped<IValidadorIdentificadorServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<INotificacionEmpresaServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<IConsultaDeudaServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<INumeroCuentaGenerador, GeneradorNumerosCriptografico>();
            builder.Services.AddScoped<INumeroTarjetaGenerador, GeneradorNumerosCriptografico>();
            builder.Services.AddScoped<IConfiguracionDistribucionPagos, ConfiguracionPagosPorAppSettings>();

            var app = builder.Build();

            // Explorador Scalar + OpenAPI: solo Development. En Producción se ocultan
            // para no exponer la superficie completa del API ni facilitar fingerprinting.
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

                    // Si la respuesta es 404 y no hay contenido, forzamos un JSON de error
                    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
                    {
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Recurso no encontrado. Verifique la ruta y los parámetros.",
                            path = context.Request.Path,
                            metodo = context.Request.Method
                        });
                        await context.Response.WriteAsync(result);
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("UnhandledException");

                    // El traceId permite correlacionar el error que ve el cliente
                    // con la entrada completa (stack trace, inner exception) en Application Logs.
                    var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
                    logger.LogError(ex,
                        "Excepción no controlada en {Path}. TraceId={TraceId}",
                        context.Request.Path, traceId);

                    if (!context.Response.HasStarted)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";

                        object payload;
                        if (app.Environment.IsDevelopment())
                        {
                            // En desarrollo sí incluimos detalles para depurar localmente.
                            payload = new
                            {
                                error = "Excepción no controlada en el banco.",
                                tipo = ex.GetType().FullName,
                                mensaje = ex.Message,
                                innerMensaje = ex.InnerException?.Message,
                                path = context.Request.Path.Value,
                                traceId
                            };
                        }
                        else
                        {
                            // En producción solo devolvemos un id de correlación; los
                            // detalles del error nunca llegan al cliente para no exponer
                            // estructura interna ni datos sensibles que el mensaje pudiera arrastrar.
                            payload = new
                            {
                                error = "Ocurrió un error procesando la solicitud. Reporte el traceId al soporte del banco.",
                                traceId
                            };
                        }

                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(payload));
                    }
                }
            });

            app.UseHttpsRedirection();

            // MIDDLEWARE PIPELINE
            app.UseRouting();                   // 1. Sabe a dónde va la petición

            // 2. Aplica la nueva política estricta conectada a las variables de entorno de Azure
            app.UseCors("ProduccionCORS");

            app.UseAuthentication();            // 3. Autenticación (Valida el JWT real)
            app.UseAuthorization();             // 4. Autorización (Aplica los [Authorize] y roles)

            app.MapControllers();               // 5. Ejecuta el controlador

            app.Run();
        }
    }
}