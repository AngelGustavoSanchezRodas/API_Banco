using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Persistencia;
using API_Banco.Domain.Entities;
using API_Banco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Infrastructure.Repositories;

public class TransaccionRepositorio(BancoDbContext context) : ITransaccionRepositorio
{
    public async Task RegistrarMovimientoPendienteAsync(int idCuenta, int idTipoTransaccion, decimal monto, DateTime fechaUtc, CancellationToken cancellationToken = default)
    {
        var transaccion = new TransaccionBanco
        {
            IdCuenta = idCuenta,
            IdTipoTransaccion = idTipoTransaccion,
            Monto = monto,
            Fecha = fechaUtc
        };
        await context.TransaccionesBanco.AddAsync(transaccion, cancellationToken);
    }

    public async Task<TransaccionBanco> CrearMovimientoPendienteAsync(int idCuenta, int idTipoTransaccion, decimal monto, DateTime fechaUtc, CancellationToken cancellationToken = default)
    {
        var transaccion = new TransaccionBanco
        {
            IdCuenta = idCuenta,
            IdTipoTransaccion = idTipoTransaccion,
            Monto = monto,
            Fecha = fechaUtc
        };

        await context.TransaccionesBanco.AddAsync(transaccion, cancellationToken);
        return transaccion;
    }

    public async Task<int> ObtenerIdUltimaTransaccionAsync(int idCuenta, DateTime fechaUtc, decimal monto, int idTipoTransaccion, CancellationToken cancellationToken = default)
    {
        var tx = await context.TransaccionesBanco
            .Where(t => t.IdCuenta == idCuenta && t.IdTipoTransaccion == idTipoTransaccion && t.Monto == monto)
            .OrderByDescending(t => t.IdTransaccion)
            .FirstOrDefaultAsync(cancellationToken);

        return tx?.IdTransaccion ?? 0;
    }

    public async Task<IReadOnlyList<TransaccionKardexItem>> ListarPorCuentaOrdenCronologicoAsync(int idCuenta, DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken cancellationToken = default)
    {
        var query = context.TransaccionesBanco
            .Include(t => t.TipoTransaccion)
            .Where(t => t.IdCuenta == idCuenta)
            .AsQueryable();

        if (desdeUtc.HasValue) query = query.Where(t => t.Fecha >= desdeUtc.Value);
        if (hastaUtc.HasValue) query = query.Where(t => t.Fecha <= hastaUtc.Value);

        return await query
            .OrderBy(t => t.Fecha)
            .Select(t => new TransaccionKardexItem(
                t.IdTransaccion, t.IdCuenta, t.Monto, t.Fecha,
                t.TipoTransaccion!.Descripcion, t.TipoTransaccion.Descripcion))
            .ToListAsync(cancellationToken);
    }
}