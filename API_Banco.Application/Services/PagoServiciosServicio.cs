using API_Banco.Application.Common;
using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services.Internos;
using API_Banco.Domain.Entities;

namespace API_Banco.Application.Services;

/// <summary>
/// Valida identificadores de servicio y ejecuta pagos con distribución 95/5, bitácora y notificación en línea a la empresa.
/// </summary>
public sealed class PagoServiciosServicio(
    IValidadorIdentificadorServicio validadorIdentificador,
    ICuentaRepositorio cuentas,
    ITransaccionRepositorio transacciones,
    ITipoTransaccionRepositorio tiposTransaccion,
    IRegistroPagoServicioRepositorio registrosPago,
    IConfiguracionDistribucionPagos distribucion,
    INotificacionEmpresaServicio notificacionEmpresa,
    IUnidadDeTrabajo unidadDeTrabajo,
    IProveedorFecha fecha) : IPagoServiciosServicio
{
    /// <inheritdoc />
    public async Task<ResultadoOperacion<ValidacionIdentificadorResultadoDto>> ValidarIdentificadorAsync(
        ValidacionIdentificadorDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(dto.Identificador))
            return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Fallo("El identificador no es válido.");

        var validacion = await validadorIdentificador
            .ValidarAsync(dto.TipoServicio, dto.Identificador.Trim(), cancellationToken)
            .ConfigureAwait(false);

        var salida = new ValidacionIdentificadorResultadoDto(
            validacion.EsValido,
            validacion.Mensaje,
            validacion.ReferenciaExterna);

        return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Ok(salida);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<PagoServicioResultadoDto>> EjecutarPagoServicioAsync(
        PagoServicioDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.IdCuentaPagadora <= 0)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La cuenta pagadora no es válida.");

        if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(dto.Identificador))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El identificador no es válido.");

        if (!ValidadoresEntrada.EsMontoValido(dto.Monto))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El monto del pago debe ser mayor que cero.");

        var validacion = await validadorIdentificador
            .ValidarAsync(dto.TipoServicio, dto.Identificador.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (!validacion.EsValido)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                validacion.Mensaje ?? "No se pudo validar el identificador ante la empresa.");

        var idTipoDebito = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.PagoServicioDebitoCuentahabiente, cancellationToken)
            .ConfigureAwait(false);
        var idTipoPrestadora = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.PagoServicioAcreditacionPrestadora, cancellationToken)
            .ConfigureAwait(false);
        var idTipoComision = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.PagoServicioComisionBanco, cancellationToken)
            .ConfigureAwait(false);

        if (idTipoDebito is null || idTipoPrestadora is null || idTipoComision is null)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "Faltan tipos de transacción configurados para pagos de servicios (débito, acreditación prestadora o comisión).");

        int idCuentaPrestadora;
        int idCuentaComisiones;
        try
        {
            idCuentaPrestadora = await distribucion
                .ObtenerIdCuentaPrestadoraAsync(dto.TipoServicio, cancellationToken)
                .ConfigureAwait(false);
            idCuentaComisiones = await distribucion
                .ObtenerIdCuentaCorrienteComisionesBancoAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "No se pudo resolver la configuración de cuentas para la distribución del pago.",
                ex.Message);
        }

        if (idCuentaPrestadora == dto.IdCuentaPagadora || idCuentaComisiones == dto.IdCuentaPagadora)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "La cuenta pagadora no puede coincidir con la cuenta prestadora o de comisiones.");

        var cuentaPagadora = await cuentas.ObtenerEntidadPorIdAsync(dto.IdCuentaPagadora, cancellationToken).ConfigureAwait(false);
        if (cuentaPagadora is null)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La cuenta pagadora no existe.");

        try
        {
            cuentaPagadora.Debitar(dto.Monto);
        }
        catch (Exception ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "Saldo insuficiente para ejecutar el pago.",
                ex.Message);
        }

        var (montoPrestadora, comisionBanco) = DistribuidorPago95Por5.Calcular(dto.Monto);
        var ahora = fecha.ObtenerUtcAhora();

        var cuentaPrestadora = await cuentas.ObtenerEntidadPorIdAsync(idCuentaPrestadora, cancellationToken).ConfigureAwait(false);
        if (cuentaPrestadora is null)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La cuenta de la empresa prestadora no existe.");

        var cuentaComisiones = await cuentas.ObtenerEntidadPorIdAsync(idCuentaComisiones, cancellationToken).ConfigureAwait(false);
        if (cuentaComisiones is null)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La cuenta de comisiones del banco no existe.");

        cuentaPrestadora.Acreditar(montoPrestadora);
        cuentaComisiones.Acreditar(comisionBanco);

        var transaccionDebito = await transacciones
            .CrearMovimientoPendienteAsync(dto.IdCuentaPagadora, idTipoDebito.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        await transacciones
            .RegistrarMovimientoPendienteAsync(idCuentaPrestadora, idTipoPrestadora.Value, montoPrestadora, ahora, cancellationToken)
            .ConfigureAwait(false);
        await transacciones
            .RegistrarMovimientoPendienteAsync(idCuentaComisiones, idTipoComision.Value, comisionBanco, ahora, cancellationToken)
            .ConfigureAwait(false);

        var registroPago = new RegistroPagoServicio
        {
            TransaccionOrigen = transaccionDebito,
            EntidadServicio = dto.TipoServicio.ToString(),
            IdentificadorServicio = dto.Identificador.Trim(),
            MontoPagado = dto.Monto
        };

        await registrosPago.RegistrarAsync(registroPago, cancellationToken).ConfigureAwait(false);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var idDebito = await transacciones
            .ObtenerIdUltimaTransaccionAsync(dto.IdCuentaPagadora, ahora, dto.Monto, idTipoDebito.Value, cancellationToken)
            .ConfigureAwait(false);
        var idPrestadora = await transacciones
            .ObtenerIdUltimaTransaccionAsync(idCuentaPrestadora, ahora, montoPrestadora, idTipoPrestadora.Value, cancellationToken)
            .ConfigureAwait(false);
        var idComision = await transacciones
            .ObtenerIdUltimaTransaccionAsync(idCuentaComisiones, ahora, comisionBanco, idTipoComision.Value, cancellationToken)
            .ConfigureAwait(false);

        var saldoPosterior = cuentaPagadora.Saldo;

        var notificacion = new NotificacionPagoEmpresaDto(
            dto.TipoServicio,
            dto.Identificador.Trim(),
            montoPrestadora,
            dto.ReferenciaCliente,
            idDebito.ToString(),
            ahora);

        var notificacionEnviada = true;
        try
        {
            await notificacionEmpresa.NotificarPagoAcreditadoAsync(notificacion, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            notificacionEnviada = false;
        }

        var resultado = new PagoServicioResultadoDto(
            idDebito,
            idPrestadora,
            idComision,
            dto.Monto,
            montoPrestadora,
            comisionBanco,
            saldoPosterior,
            ahora,
            notificacionEnviada);

        return ResultadoOperacion<PagoServicioResultadoDto>.Ok(resultado);
    }
}
