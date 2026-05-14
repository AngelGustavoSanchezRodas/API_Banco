using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Persistencia;
using API_Banco.Domain.Entities;
using API_Banco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Infrastructure.Repositories;

public class TarjetaDebitoRepositorio(BancoDbContext context) : ITarjetaDebitoRepositorio
{
    public async Task RegistrarTarjetaPendienteAsync(int idCuenta, string numeroTarjeta, string pin, DateTime fechaVencimiento, int idEstado, CancellationToken cancellationToken = default)
    {
        var tarjeta = new TarjetaDebito
        {
            IdCuenta = idCuenta,
            NumeroTarjeta = numeroTarjeta,
            PinHash = pin, // En un entorno real, aquí se hashea (ej. BCrypt)
            FechaVencimiento = fechaVencimiento,
            IdEstado = idEstado
        };
        await context.TarjetasDebito.AddAsync(tarjeta, cancellationToken);
    }

    public async Task<TarjetaDebitoCreada?> ObtenerUltimaPorCuentaAsync(int idCuenta, CancellationToken cancellationToken = default)
    {
        return await context.TarjetasDebito
            .Where(t => t.IdCuenta == idCuenta)
            .OrderByDescending(t => t.IdTarjeta)
            .Select(t => new TarjetaDebitoCreada(t.IdTarjeta, t.NumeroTarjeta, t.IdCuenta, t.FechaVencimiento))
            .FirstOrDefaultAsync(cancellationToken);
    }
}