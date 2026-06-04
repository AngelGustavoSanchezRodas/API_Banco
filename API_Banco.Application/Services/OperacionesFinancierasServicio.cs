using API_Banco.Application.Common;
using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Operaciones;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services.Internos;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Application.Services;

/// <summary>
/// Implementa depósitos, retiros y consulta de saldo con registro en bitácora mediante transacciones bancarias.
/// </summary>
public sealed class OperacionesFinancierasServicio(
    ICuentaRepositorio cuentas,
    ITransaccionRepositorio transacciones,
    ITipoTransaccionRepositorio tiposTransaccion,
    IEstadoRepositorio estados,
    ITarjetaDebitoRepositorio tarjetas,
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

        // Blindaje contra errores de captura: no se aceptan depósitos por
        // ventanilla que excedan el tope operativo. Para montos mayores
        // existe el flujo manual de cumplimiento con doble validación.
        if (!ValidadoresEntrada.EstaDentroDelTopeOperacion(dto.Monto))
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                $"El monto del depósito no puede exceder Q{ValidadoresEntrada.MontoMaximoOperacion:N2} por operación.");

        var idTipo = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.Deposito, cancellationToken)
            .ConfigureAwait(false);
        if (idTipo is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Tipo de transacción DEPOSITO no configurado.");

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        if (idEstadoActivo is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Estado ACTIVO no configurado para cuentas.");

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        if (cuenta.IdEstado != idEstadoActivo.Value)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no está activa.");

        cuenta.Acreditar(dto.Monto);

        var ahora = fecha.ObtenerUtcAhora();
        var transaccion = await transacciones
            .CrearMovimientoPendienteAsync(dto.IdCuenta, idTipo.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "La transacción no pudo completarse porque el saldo fue modificado por otra operación simultánea. Por favor, verifique su saldo e intente de nuevo.");
        }

        var resultado = new MovimientoFinancieroResultadoDto(
            transaccion.IdTransaccion,
            dto.IdCuenta,
            dto.Monto,
            cuenta.Saldo,
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

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        if (idEstadoActivo is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Estado ACTIVO no configurado para cuentas.");

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        if (cuenta.IdEstado != idEstadoActivo.Value)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no está activa.");

        if (cuenta.Saldo < dto.Monto)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Fondos insuficientes para el retiro.");

        cuenta.Debitar(dto.Monto);

        var ahora = fecha.ObtenerUtcAhora();
        var transaccion = await transacciones
            .CrearMovimientoPendienteAsync(dto.IdCuenta, idTipo.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "La transacción no pudo completarse porque el saldo fue modificado por otra operación simultánea. Por favor, verifique su saldo e intente de nuevo.");
        }

        var resultado = new MovimientoFinancieroResultadoDto(
            transaccion.IdTransaccion,
            dto.IdCuenta,
            dto.Monto,
            cuenta.Saldo,
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

        // Mismo tope que aplica a depósitos posteriores: la activación con
        // saldo inicial no puede convertirse en una vía para bypassar el
        // límite operativo del banco.
        if (!ValidadoresEntrada.EstaDentroDelTopeOperacion(montoDeposito))
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                $"El depósito de apertura no puede exceder Q{ValidadoresEntrada.MontoMaximoOperacion:N2}.");

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo).ConfigureAwait(false);
        if (idEstadoActivo is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Estado ACTIVO no configurado para cuentas.");

        var idEstadoPendiente = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.PendienteActivacion).ConfigureAwait(false);
        if (idEstadoPendiente is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Estado PENDIENTE_ACTIVACION no configurado para cuentas.");

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(idCuenta).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no existe.");

        if (cuenta.IdEstado != idEstadoPendiente.Value)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta no está pendiente de activación.");

        var idTipoDeposito = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.Deposito)
            .ConfigureAwait(false);
        if (idTipoDeposito is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Tipo de transacción DEPOSITO no configurado.");

        cuenta.IdEstado = idEstadoActivo.Value;
        cuenta.Acreditar(montoDeposito);

        var ahora = fecha.ObtenerUtcAhora();
        var transaccion = await transacciones
            .CrearMovimientoPendienteAsync(idCuenta, idTipoDeposito.Value, montoDeposito, ahora)
            .ConfigureAwait(false);

        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "La transacción no pudo completarse porque el saldo fue modificado por otra operación simultánea. Por favor, verifique su saldo e intente de nuevo.");
        }

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

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo).ConfigureAwait(false);
        if (idEstadoActivo is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Estado ACTIVO no configurado para cuentas.");

        var idTipoOrigen = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.TransferenciaOrigen)
            .ConfigureAwait(false);
        var idTipoDestino = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.TransferenciaDestino)
            .ConfigureAwait(false);
        if (idTipoOrigen is null || idTipoDestino is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "Tipos de transacción TRANSFERENCIA_ORIGEN / TRANSFERENCIA_DESTINO no configurados.");

        var idEstadoPendiente = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.PendienteActivacion).ConfigureAwait(false);
        if (idEstadoPendiente is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Estado PENDIENTE_ACTIVACION no configurado.");

        var cuentaOrigen = await cuentas.ObtenerEntidadPorIdAsync(idCuentaOrigen).ConfigureAwait(false);
        if (cuentaOrigen is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta origen no existe.");

        var cuentaDestino = await cuentas.ObtenerEntidadPorIdAsync(idCuentaDestino).ConfigureAwait(false);
        if (cuentaDestino is null)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta destino no existe.");

        // Origen DEBE estar ACTIVA (no puede operar si está INACTIVA o PENDIENTE).
        if (cuentaOrigen.IdEstado != idEstadoActivo.Value)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta de origen no está activa.");

        // Destino puede estar ACTIVA o INACTIVA (recibir dinero sigue permitido aunque
        // el dueño no pueda operar). Lo único que rechazamos es PENDIENTE_ACTIVACION,
        // porque la cuenta aún no fue habilitada por el banco.
        if (cuentaDestino.IdEstado == idEstadoPendiente.Value)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("La cuenta de destino aún no ha sido activada.");

        if (cuentaOrigen.Saldo < monto)
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo("Saldo insuficiente.");

        cuentaOrigen.Debitar(monto);
        cuentaDestino.Acreditar(monto);

        var referenciaBase = Guid.NewGuid().ToString("N");
        var detalle = string.IsNullOrWhiteSpace(descripcion) ? "TRANSFERENCIA" : descripcion.Trim();
        var referenciaVinculante = $"{referenciaBase}:{detalle}";
        var ahora = fecha.ObtenerUtcAhora();

        var transaccionOrigen = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaOrigen, idTipoOrigen.Value, monto, ahora)
            .ConfigureAwait(false);
        transaccionOrigen.ReferenciaVinculante = referenciaVinculante;

        var transaccionDestino = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaDestino, idTipoDestino.Value, monto, ahora)
            .ConfigureAwait(false);
        transaccionDestino.ReferenciaVinculante = referenciaVinculante;

        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Fallo(
                "La transacción no pudo completarse porque el saldo fue modificado por otra operación simultánea. Por favor, verifique su saldo e intente de nuevo.");
        }

        var resultado = new MovimientoFinancieroResultadoDto(
            transaccionOrigen.IdTransaccion,
            idCuentaOrigen,
            monto,
            cuentaOrigen.Saldo,
            ahora);

        return ResultadoOperacion<MovimientoFinancieroResultadoDto>.Ok(resultado);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<CambioEstadoCuentaDto>> SuspenderCuentaAsync(
        int idCuenta,
        CancellationToken cancellationToken = default)
    {
        if (idCuenta <= 0)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("La cuenta no es válida.");

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        var idEstadoInactivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Inactivo, cancellationToken).ConfigureAwait(false);
        if (idEstadoActivo is null || idEstadoInactivo is null)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("Estados ACTIVO / INACTIVO no configurados.");

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(idCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("La cuenta no existe.");

        if (await cuentas.EsCuentaInternaAsync(idCuenta, cancellationToken).ConfigureAwait(false))
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("Las cuentas internas del banco no pueden ser suspendidas.");

        if (cuenta.IdEstado != idEstadoActivo.Value)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("Sólo se puede suspender una cuenta que está ACTIVA.");

        var estadoAnterior = cuenta.IdEstado;
        cuenta.IdEstado = idEstadoInactivo.Value;

        // Bloquear todas las tarjetas activas (si una cuenta queda suspendida,
        // su(s) tarjeta(s) deben quedar inservibles).
        var tarjetasBloqueadas = await tarjetas
            .BloquearTarjetasActivasDeCuentaAsync(idCuenta, idEstadoActivo.Value, idEstadoInactivo.Value, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo(
                "La cuenta fue modificada por otra operación. Reintenta.");
        }

        var dto = new CambioEstadoCuentaDto(
            cuenta.IdCuenta,
            cuenta.NoCuenta,
            estadoAnterior,
            idEstadoInactivo.Value,
            CodigosEstado.Inactivo,
            tarjetasBloqueadas,
            fecha.ObtenerUtcAhora());

        return ResultadoOperacion<CambioEstadoCuentaDto>.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<CambioEstadoCuentaDto>> ReactivarCuentaAsync(
        int idCuenta,
        CancellationToken cancellationToken = default)
    {
        if (idCuenta <= 0)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("La cuenta no es válida.");

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        var idEstadoInactivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Inactivo, cancellationToken).ConfigureAwait(false);
        if (idEstadoActivo is null || idEstadoInactivo is null)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("Estados ACTIVO / INACTIVO no configurados.");

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(idCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("La cuenta no existe.");

        if (await cuentas.EsCuentaInternaAsync(idCuenta, cancellationToken).ConfigureAwait(false))
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("Las cuentas internas del banco no pueden cambiar de estado.");

        if (cuenta.IdEstado != idEstadoInactivo.Value)
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo("Sólo se puede reactivar una cuenta que está INACTIVA.");

        var estadoAnterior = cuenta.IdEstado;
        cuenta.IdEstado = idEstadoActivo.Value;

        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion<CambioEstadoCuentaDto>.Fallo(
                "La cuenta fue modificada por otra operación. Reintenta.");
        }

        var dto = new CambioEstadoCuentaDto(
            cuenta.IdCuenta,
            cuenta.NoCuenta,
            estadoAnterior,
            idEstadoActivo.Value,
            CodigosEstado.Activo,
            0,
            fecha.ObtenerUtcAhora());

        return ResultadoOperacion<CambioEstadoCuentaDto>.Ok(dto);
    }
}
