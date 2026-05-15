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

            // 1. Configurar CORS (Obligatorio para que Next.js no sea bloqueado)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("NextJsPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:3000") // Tu frontend
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // 2. Extraer la cadena de conexión
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 3. Registrar el DbContext con Pomelo MySQL
            builder.Services.AddDbContext<BancoDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            );

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

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

            // 6. Inyección de Servicios Externos (Mocks e Integraciones)
            builder.Services.AddHttpClient("UniversidadApi", client =>
                client.BaseAddress = new Uri("https://sistemapagosuniversidad.azurewebsites.net/"));
            builder.Services.AddScoped<GestorIntegracionServicios>();
            builder.Services.AddScoped<IValidadorIdentificadorServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<INotificacionEmpresaServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<IConsultaDeudaServicio>(sp => sp.GetRequiredService<GestorIntegracionServicios>());
            builder.Services.AddScoped<INumeroCuentaGenerador, GeneradoresMock>();
            builder.Services.AddScoped<INumeroTarjetaGenerador, GeneradoresMock>();
            builder.Services.AddScoped<IConfiguracionDistribucionPagos, ConfiguracionPagosMock>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()  || app.Environment.IsProduction())
            {
                app.MapScalarApiReference();
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            // 6. Activar CORS (Debe ir antes del Authorization)
            app.UseCors("NextJsPolicy");

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
