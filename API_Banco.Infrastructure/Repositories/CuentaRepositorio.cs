using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Persistencia;
using API_Banco.Domain.Entities;
using API_Banco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Infrastructure.Repositories;

public class CuentaRepositorio(BancoDbContext context) : ICuentaRepositorio
{
    public async Task<CuentaResumen?> ObtenerPorIdAsync(int idCuenta, CancellationToken cancellationToken = default)
    {
        return await context.Cuentas
            .AsNoTracking()
            .Where(c => c.IdCuenta == idCuenta)
            .Select(c => new CuentaResumen(c.IdCuenta, c.NoCuenta, c.Saldo, c.IdCliente, c.IdEstado))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CuentaResumen?> ObtenerPorNumeroAsync(string noCuenta, CancellationToken cancellationToken = default)
    {
        return await context.Cuentas
            .AsNoTracking()
            .Where(c => c.NoCuenta == noCuenta)
            .Select(c => new CuentaResumen(c.IdCuenta, c.NoCuenta, c.Saldo, c.IdCliente, c.IdEstado))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Cuenta?> ObtenerEntidadPorIdAsync(int idCuenta, CancellationToken cancellationToken = default)
    {
        return await context.Cuentas.FirstOrDefaultAsync(c => c.IdCuenta == idCuenta, cancellationToken);
    }

    public async Task<bool> PerteneceAClienteAsync(int idCuenta, int idCliente, CancellationToken cancellationToken = default)
    {
        return await context.Cuentas
            .AnyAsync(c => c.IdCuenta == idCuenta && c.IdCliente == idCliente, cancellationToken);
    }

    public async Task RegistrarCuentaPendienteAsync(string noCuenta, Cliente cliente, int idTipoCuenta, int idEstado, decimal saldoInicial, CancellationToken cancellationToken = default)
    {
        var cuenta = new Cuenta
        {
            NoCuenta = noCuenta,
            Saldo = saldoInicial,
            Cliente = cliente,
            IdTipoCuenta = idTipoCuenta,
            IdEstado = idEstado
        };
        await context.Cuentas.AddAsync(cuenta, cancellationToken);
    }

    public async Task<bool> IntentarAplicarDeltaSaldoAsync(int idCuenta, decimal delta, CancellationToken cancellationToken = default)
    {
        var cuenta = await context.Cuentas.FirstOrDefaultAsync(c => c.IdCuenta == idCuenta, cancellationToken);
        if (cuenta == null) return false;

        // Regla de negocio: No se puede retirar dinero que no se tiene
        if (cuenta.Saldo + delta < 0) return false;

        cuenta.Saldo += delta;
        // El SaveChangesAsync se ejecutará luego desde la Unidad de Trabajo
        return true;
    }
}
