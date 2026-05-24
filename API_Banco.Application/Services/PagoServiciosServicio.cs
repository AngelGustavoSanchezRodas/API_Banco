using API_Banco.Application.Common;
using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services.Internos;
using API_Banco.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace API_Banco.Application.Services;

/// <summary>
/// Valida identificadores de servicio y ejecuta pagos con distribución 95/5, bitácora y notificación en línea a la empresa.
/// </summary>
public sealed class PagoServiciosServicio(
    IValidadorIdentificadorServicio validadorIdentificador,
    IConsultaDeudaServicio consultaDeudaServicio,
    ICuentaRepositorio cuentas,
    ITarjetaDebitoRepositorio tarjetas,
    ITransaccionRepositorio transacciones,
    ITipoTransaccionRepositorio tiposTransaccion,
    IRegistroPagoServicioRepositorio registrosPago,
    IConfiguracionDistribucionPagos distribucion,
    INotificacionEmpresaServicio notificacionEmpresa,
    IUnidadDeTrabajo unidadDeTrabajo,
    IProveedorFecha fecha,
    ILogger<PagoServiciosServicio> logger) : IPagoServiciosServicio
{
    /// <inheritdoc />
    public async Task<ResultadoOperacion<ValidacionIdentificadorResultadoDto>> ValidarIdentificadorAsync(
        ValidacionIdentificadorDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(dto.Identificador))
            return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Fallo("El identificador no es válido.");

        ResultadoValidacion validacion;
        try
        {
            validacion = await validadorIdentificador
                .ValidarAsync(dto.TipoServicio, dto.Identificador.Trim(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Fallo(
                "No se pudo validar el identificador en el proveedor externo.",
                ex.Message);
        }
        catch (JsonException ex)
        {
            return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Fallo(
                "La respuesta del proveedor externo no tiene el formato esperado.",
                ex.Message);
        }

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
        if (string.IsNullOrWhiteSpace(dto.NumeroTarjeta))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El número de tarjeta es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Pin))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El PIN es obligatorio.");

        if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(dto.Identificador))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El identificador no es válido.");

        if (!ValidadoresEntrada.EsMontoValido(dto.Monto))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El monto del pago debe ser mayor que cero.");

        ResultadoValidacion validacion;
        try
        {
            validacion = await validadorIdentificador
                .ValidarAsync(dto.TipoServicio, dto.Identificador.Trim(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "No se pudo validar el identificador en el proveedor externo.",
                ex.Message);
        }
        catch (JsonException ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "La respuesta del proveedor externo no tiene el formato esperado.",
                ex.Message);
        }
        if (!validacion.EsValido)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                validacion.Mensaje ?? "No se pudo validar el identificador ante la empresa.");

        decimal deudaPendiente;
        try
        {
            deudaPendiente = await consultaDeudaServicio
                .ConsultarDeudaAsync(dto.TipoServicio, dto.Identificador.Trim(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "No se pudo consultar la deuda pendiente del servicio.",
                ex.Message);
        }

        if (deudaPendiente <= 0)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El servicio no tiene deuda pendiente.");

        if (dto.Monto != deudaPendiente)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                $"El monto debe coincidir con la deuda pendiente (Q{deudaPendiente:N2}).");

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

        var tarjeta = await tarjetas.ObtenerPorNumeroAsync(dto.NumeroTarjeta.Trim(), cancellationToken).ConfigureAwait(false);
        if (tarjeta is null || tarjeta.IdEstado != 1)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La tarjeta no existe o está inactiva.");

        if (!string.Equals(tarjeta.PinHash, dto.Pin.Trim(), StringComparison.Ordinal))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("PIN incorrecto.");

        if (tarjeta.Cuenta is null)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La tarjeta no tiene una cuenta asociada.");

        var cuentaPagadora = tarjeta.Cuenta;

        try
        {
            cuentaPagadora.Debitar(dto.Monto);
        }
        catch (Exception ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "Fondos insuficientes.",
                ex.Message);
        }

        if (idCuentaPrestadora == cuentaPagadora.IdCuenta || idCuentaComisiones == cuentaPagadora.IdCuenta)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "La cuenta pagadora no puede coincidir con la cuenta prestadora o de comisiones.");

        var (montoPrestadora, comisionBanco) = DistribuidorPago95Por5.Calcular(dto.Monto);
        var codigoEntidadServicio = CodigosEntidadServicio.ParaRegistro(dto.TipoServicio);
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
            .CrearMovimientoPendienteAsync(cuentaPagadora.IdCuenta, idTipoDebito.Value, dto.Monto, ahora, cancellationToken)
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
            EntidadServicio = codigoEntidadServicio,
            IdentificadorServicio = dto.Identificador.Trim(),
            MontoTotalPagado = dto.Monto,
            MontoEmpresa95 = montoPrestadora,
            ComisionBanco5 = comisionBanco
        };

        await registrosPago.RegistrarAsync(registroPago, cancellationToken).ConfigureAwait(false);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var idDebito = await transacciones
            .ObtenerIdUltimaTransaccionAsync(cuentaPagadora.IdCuenta, ahora, dto.Monto, idTipoDebito.Value, cancellationToken)
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
            dto.Monto,
            dto.ReferenciaCliente,
            idDebito.ToString(),
            ahora);

        // IMPORTANTE: la notificación a la empresa (callback) NO se puede deshacer
        // porque el débito ya está confirmado en BD. Si falla, igual respondemos
        // al portal con éxito de cobro pero marcamos notificacionEnviada=false y
        // dejamos rastro completo del error en Application Logs para soporte.
        var notificacionEnviada = true;
        try
        {
            await notificacionEmpresa.NotificarPagoAcreditadoAsync(notificacion, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            notificacionEnviada = false;
            logger.LogError(
                ex,
                "Notificación de pago acreditado FALLÓ | tipoServicio={Tipo} identificador={Identificador} monto={Monto} referencia={Referencia}. " +
                "El cobro al cuentahabiente ya está confirmado. Se requiere conciliación manual con la empresa prestadora.",
                notificacion.TipoServicio,
                notificacion.Identificador,
                notificacion.MontoAcreditado,
                notificacion.ReferenciaTransaccionBanco);
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

    /// <inheritdoc />
    public async Task<ResultadoOperacion<decimal>> ConsultarDeudaAsync(int tipoServicio, string identificador)
    {
        if (!Enum.IsDefined(typeof(TipoServicioPublico), tipoServicio))
            return ResultadoOperacion<decimal>.Fallo("El tipo de servicio no es válido.");

        if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(identificador))
            return ResultadoOperacion<decimal>.Fallo("El identificador no es válido.");

        var tipo = (TipoServicioPublico)tipoServicio;
        var identificadorLimpio = identificador.Trim();

        try
        {
            var deuda = await consultaDeudaServicio
                .ConsultarDeudaAsync(tipo, identificadorLimpio)
                .ConfigureAwait(false);
            return ResultadoOperacion<decimal>.Ok(deuda);
        }
        catch (HttpRequestException ex)
        {
            return ResultadoOperacion<decimal>.Fallo(
                "No se pudo consultar la deuda en el proveedor externo.",
                ex.Message);
        }
        catch (JsonException ex)
        {
            return ResultadoOperacion<decimal>.Fallo(
                "La respuesta del proveedor externo no tiene el formato esperado.",
                ex.Message);
        }
    }
}
