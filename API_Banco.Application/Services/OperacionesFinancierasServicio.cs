using API_Banco.Application.Common;
using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Operaciones;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services.Internos;

namespace API_Banco.Application.Services;

/// <summary>
/// Implementa depósitos, retiros y consulta de saldo con registro en bitácora mediante transacciones bancarias.
/// </summary>
public sealed class OperacionesFinancierasServicio(
    ICuentaRepositorio cuentas,
    ITransaccionRepositorio transacciones,
    ITipoTransaccionRepositorio tiposTransaccion,
    IUnidadDeTrabajo unidadDeTrabajo,
    IProveedorFecha fecha) : IOperacionesFinancierasServicio
{
    /// <inheritdoc />
    public async Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> DepositarAsync(
        DepositoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ValidadoresEntrada.EsMontoValido(dto.Monto))
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("El monto del depósito debe ser mayor que cero.");

        var idTipo = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.Deposito, cancellationToken)
            .ConfigureAwait(false);
        if (idTipo is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Tipo de transacción DEPOSITO no configurado.");

        var cuenta = await cuentas.ObtenerPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        var aplicado = await cuentas
            .IntentarAplicarDeltaSaldoAsync(dto.IdCuenta, dto.Monto, cancellationToken)
            .ConfigureAwait(false);
        if (!aplicado)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("No fue posible aplicar el depósito.");

        var ahora = fecha.ObtenerUtcAhora();
        await transacciones
            .RegistrarMovimientoPendienteAsync(dto.IdCuenta, idTipo.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var idTransaccion = await transacciones
            .ObtenerIdUltimaTransaccionAsync(dto.IdCuenta, ahora, dto.Monto, idTipo.Value, cancellationToken)
            .ConfigureAwait(false);

        var actualizada = await cuentas.ObtenerPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        var saldo = actualizada?.Saldo ?? cuenta.Saldo + dto.Monto;

        var resultado = new MovimientoFinancieroResultadoDto(
            idTransaccion,
            dto.IdCuenta,
            dto.Monto,
            saldo,
            ahora);

        return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Ok(resultado);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> RetirarAsync(
        RetiroDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ValidadoresEntrada.EsMontoValido(dto.Monto))
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("El monto del retiro debe ser mayor que cero.");

        var idTipo = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.Retiro, cancellationToken)
            .ConfigureAwait(false);
        if (idTipo is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Tipo de transacción RETIRO no configurado.");

        var cuenta = await cuentas.ObtenerPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        if (cuenta.Saldo < dto.Monto)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "Fondos insuficientes para el retiro.",
                $"Saldo disponible: {cuenta.Saldo:0.00}, solicitado: {dto.Monto:0.00}.");

        var aplicado = await cuentas
            .IntentarAplicarDeltaSaldoAsync(dto.IdCuenta, -dto.Monto, cancellationToken)
            .ConfigureAwait(false);
        if (!aplicado)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "No fue posible completar el retiro por disponibilidad de saldo en tiempo real.");

        var ahora = fecha.ObtenerUtcAhora();
        await transacciones
            .RegistrarMovimientoPendienteAsync(dto.IdCuenta, idTipo.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var idTransaccion = await transacciones
            .ObtenerIdUltimaTransaccionAsync(dto.IdCuenta, ahora, dto.Monto, idTipo.Value, cancellationToken)
            .ConfigureAwait(false);

        var actualizada = await cuentas.ObtenerPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        var saldo = actualizada?.Saldo ?? cuenta.Saldo - dto.Monto;

        var resultado = new MovimientoFinancieroResultadoDto(
            idTransaccion,
            dto.IdCuenta,
            dto.Monto,
            saldo,
            ahora);

        return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Ok(resultado);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<ConsultaSaldoDto>> ConsultarSaldoDisponibleAsync(
        int idCuenta,
        CancellationToken cancellationToken = default)
    {
        var cuenta = await cuentas.ObtenerPorIdAsync(idCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<ConsultaSaldoDto>.Fallo("La cuenta no existe.");

        var dto = new ConsultaSaldoDto(idCuenta, cuenta.NoCuenta, cuenta.Saldo, fecha.ObtenerUtcAhora());
        return ResultadoOperacion<ConsultaSaldoDto>.Ok(dto);
    }
}
