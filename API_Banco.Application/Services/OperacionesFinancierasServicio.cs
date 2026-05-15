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

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        cuenta.Acreditar(dto.Monto);

        var ahora = fecha.ObtenerUtcAhora();
        await transacciones
            .RegistrarMovimientoPendienteAsync(dto.IdCuenta, idTipo.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var idTransaccion = await transacciones
            .ObtenerIdUltimaTransaccionAsync(dto.IdCuenta, ahora, dto.Monto, idTipo.Value, cancellationToken)
            .ConfigureAwait(false);

        var saldo = cuenta.Saldo;

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

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        try
        {
            cuenta.Debitar(dto.Monto);
        }
        catch (InvalidOperationException ex)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "Fondos insuficientes para el retiro.",
                ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(ex.Message);
        }

        var ahora = fecha.ObtenerUtcAhora();
        await transacciones
            .RegistrarMovimientoPendienteAsync(dto.IdCuenta, idTipo.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var idTransaccion = await transacciones
            .ObtenerIdUltimaTransaccionAsync(dto.IdCuenta, ahora, dto.Monto, idTipo.Value, cancellationToken)
            .ConfigureAwait(false);

        var saldo = cuenta.Saldo;

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

    /// <inheritdoc />
    public async Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> ActivarCuentaConDepositoAsync(
        int idCuenta,
        decimal montoDeposito)
    {
        if (idCuenta <= 0)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no es válida.");

        if (!ValidadoresEntrada.EsMontoValido(montoDeposito))
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("El monto del depósito debe ser mayor que cero.");

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(idCuenta).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        if (cuenta.IdEstado != 3)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no está pendiente de activación.");

        var idTipoDeposito = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.Deposito)
            .ConfigureAwait(false);
        if (idTipoDeposito is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Tipo de transacción DEPOSITO no configurado.");

        cuenta.IdEstado = 1;
        try
        {
            cuenta.Acreditar(montoDeposito);
        }
        catch (InvalidOperationException ex)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(ex.Message);
        }

        var ahora = fecha.ObtenerUtcAhora();
        var transaccion = await transacciones
            .CrearMovimientoPendienteAsync(idCuenta, idTipoDeposito.Value, montoDeposito, ahora)
            .ConfigureAwait(false);

        await unidadDeTrabajo.GuardarCambiosAsync().ConfigureAwait(false);

        var resultado = new MovimientoFinancieroResultadoDto(
            transaccion.IdTransaccion,
            idCuenta,
            montoDeposito,
            cuenta.Saldo,
            ahora);

        return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Ok(resultado);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> TransferirAsync(
        int idCuentaOrigen,
        int idCuentaDestino,
        decimal monto,
        string descripcion)
    {
        if (idCuentaOrigen <= 0 || idCuentaDestino <= 0)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Las cuentas de transferencia no son válidas.");

        if (idCuentaOrigen == idCuentaDestino)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta origen y destino no pueden ser la misma.");

        if (!ValidadoresEntrada.EsMontoValido(monto))
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("El monto de la transferencia debe ser mayor que cero.");

        var cuentaOrigen = await cuentas.ObtenerEntidadPorIdAsync(idCuentaOrigen).ConfigureAwait(false);
        if (cuentaOrigen is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta origen no existe.");

        var cuentaDestino = await cuentas.ObtenerEntidadPorIdAsync(idCuentaDestino).ConfigureAwait(false);
        if (cuentaDestino is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta destino no existe.");

        if (cuentaOrigen.IdEstado != 1 || cuentaDestino.IdEstado != 1)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Ambas cuentas deben estar activas.");

        try
        {
            cuentaOrigen.Debitar(monto);
            cuentaDestino.Acreditar(monto);
        }
        catch (InvalidOperationException ex)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(ex.Message);
        }

        var referenciaBase = Guid.NewGuid().ToString("N");
        var detalle = string.IsNullOrWhiteSpace(descripcion) ? "TRANSFERENCIA" : descripcion.Trim();
        var referenciaVinculante = $"{referenciaBase}:{detalle}";
        var ahora = fecha.ObtenerUtcAhora();

        var transaccionOrigen = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaOrigen, 6, monto, ahora)
            .ConfigureAwait(false);
        transaccionOrigen.ReferenciaVinculante = referenciaVinculante;

        var transaccionDestino = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaDestino, 7, monto, ahora)
            .ConfigureAwait(false);
        transaccionDestino.ReferenciaVinculante = referenciaVinculante;

        await unidadDeTrabajo.GuardarCambiosAsync().ConfigureAwait(false);

        var resultado = new MovimientoFinancieroResultadoDto(
            transaccionOrigen.IdTransaccion,
            idCuentaOrigen,
            monto,
            cuentaOrigen.Saldo,
            ahora);

        return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Ok(resultado);
    }
}
