using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services;
using API_Banco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace API_Banco
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Extraer la cadena de conexión del appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 2. Registrar el DbContext con Pomelo MySQL
            builder.Services.AddDbContext<BancoDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString) 
                )
            );

            // Add services to the container.

            builder.Services.AddControllers();
           
            builder.Services.AddOpenApi();

            //
            builder.Services.AddScoped<ICuentahabienteServicio, CuentahabienteServicio>();
            builder.Services.AddScoped<IOperacionesFinancierasServicio, OperacionesFinancierasServicio>();
            builder.Services.AddScoped<IPagoServiciosServicio, PagoServiciosServicio>();
            builder.Services.AddScoped<IBitacoraServicio, BitacoraServicio>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapScalarApiReference();
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
