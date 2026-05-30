using API_Banco.Application.Common;
using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services.Internos;
using Microsoft.EntityFrameworkCore;
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
    IHasherCredenciales hasher,
    IUnidadDeTrabajo unidadDeTrabajo,
    IProveedorFecha fecha,
    ILogger<PagoServiciosServicio> logger) : IPagoServiciosServicio
{
    /// <inheritdoc />
    public async Task<ResultadoOperacion<ValidacionIdentificadorResultadoDto>> ValidarIdentificadorAsync(
        ValidacionIdentificadorDto dto,
        CancellationToken cancellationToken = default)
    {
        var identificador = dto.Identificador.Trim();
        if (dto.TipoServicio == TipoServicioPublico.Telefonia)
        {
            if (!TelefoniaIdentificador.TryNormalizar(identificador, out var digitos))
            {
                return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Fallo(
                    "El número telefónico debe tener entre 8 y 15 dígitos (solo dígitos; se permiten espacios, guiones o paréntesis como separadores).");
            }

            identificador = digitos;
        }
        else if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(identificador))
        {
            return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Fallo("El identificador no es válido.");
        }

        ResultadoValidacion validacion;
        try
        {
            validacion = await validadorIdentificador
                .ValidarAsync(dto.TipoServicio, identificador, cancellationToken)
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
        catch (NotSupportedException ex)
        {
            return ResultadoOperacion<ValidacionIdentificadorResultadoDto>.Fallo(ex.Message);
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

        // Mes/año son opcionales por compatibilidad con clientes antiguos,
        // pero si vienen DEBEN venir ambos y con rangos sanos. Si solo uno
        // está presente lo tratamos como entrada inválida.
        var enviaMes = dto.MesVencimiento.HasValue;
        var enviaAnio = dto.AnioVencimiento.HasValue;
        if (enviaMes ^ enviaAnio)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "Debe enviar tanto el mes como el año de vencimiento.");

        if (enviaMes && dto.MesVencimiento is < 1 or > 12)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El mes de vencimiento debe estar entre 1 y 12.");

        if (enviaAnio && (dto.AnioVencimiento < 2000 || dto.AnioVencimiento > 2100))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El año de vencimiento no es válido.");

        var identificador = dto.Identificador.Trim();
        if (dto.TipoServicio == TipoServicioPublico.Telefonia)
        {
            if (!TelefoniaIdentificador.TryNormalizar(identificador, out var digitos))
            {
                return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                    "El número telefónico debe tener entre 8 y 15 dígitos (solo dígitos; se permiten espacios, guiones o paréntesis como separadores).");
            }

            identificador = digitos;
        }
        else if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(identificador))
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El identificador no es válido.");
        }

        if (!ValidadoresEntrada.EsMontoValido(dto.Monto))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El monto del pago debe ser mayor que cero.");

        ResultadoValidacion validacion;
        try
        {
            validacion = await validadorIdentificador
                .ValidarAsync(dto.TipoServicio, identificador, cancellationToken)
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
        catch (NotSupportedException ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(ex.Message);
        }

        if (!validacion.EsValido)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                validacion.Mensaje ?? "No se pudo validar el identificador ante la empresa.");

        if (dto.TipoServicio == TipoServicioPublico.Telefonia && !string.IsNullOrWhiteSpace(validacion.ReferenciaExterna))
            identificador = validacion.ReferenciaExterna;

        decimal deudaPendiente;
        try
        {
            deudaPendiente = await consultaDeudaServicio
                .ConsultarDeudaAsync(dto.TipoServicio, identificador, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "No se pudo consultar la deuda pendiente del servicio.",
                ex.Message);
        }

        if (dto.TipoServicio == TipoServicioPublico.Telefonia)
        {
            // Postpago: la API de telefonía envía el monto de la factura; debe coincidir con la deuda consultada.
            // Prepago (recarga): deuda 0 y monto libre enviado por el portal.
            if (deudaPendiente > 0 && dto.Monto != deudaPendiente)
            {
                return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                    $"El monto debe coincidir con la deuda pendiente (Q{deudaPendiente:N2}).");
            }
        }
        else
        {
            if (deudaPendiente <= 0)
                return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("El servicio no tiene deuda pendiente.");

            if (dto.Monto != deudaPendiente)
            {
                return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                    $"El monto debe coincidir con la deuda pendiente (Q{deudaPendiente:N2}).");
            }
        }

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

        var pinIngresado = dto.Pin.Trim();
        if (!hasher.Verificar(pinIngresado, tarjeta.PinHash))
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("PIN incorrecto.");

        // Migración transparente: si el PIN estaba almacenado en texto plano y la
        // verificación cayó por el camino legacy, lo rehasheamos ahora aprovechando
        // que la entidad ya está siendo trackeada por EF Core para este pago.
        if (!hasher.EsHashValido(tarjeta.PinHash))
            tarjeta.PinHash = hasher.Hashear(pinIngresado);

        // Si el cliente envió la fecha de vencimiento, la validamos contra los
        // datos impresos en la tarjeta. No exponemos cuál de los dos no coincide
        // (mes vs. año) para no dar pistas a un atacante que esté probando tarjetas.
        if (enviaMes && enviaAnio)
        {
            if (tarjeta.FechaVencimiento.Month != dto.MesVencimiento ||
                tarjeta.FechaVencimiento.Year != dto.AnioVencimiento)
                return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                    "La fecha de vencimiento no coincide con la tarjeta.");

            // La tarjeta es válida hasta el último día del mes de vencimiento.
            var ahoraVencimiento = fecha.ObtenerUtcAhora();
            var ultimoDiaMes = new DateTime(
                tarjeta.FechaVencimiento.Year,
                tarjeta.FechaVencimiento.Month,
                DateTime.DaysInMonth(tarjeta.FechaVencimiento.Year, tarjeta.FechaVencimiento.Month),
                23, 59, 59, DateTimeKind.Utc);
            if (ultimoDiaMes < ahoraVencimiento)
                return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La tarjeta está vencida.");
        }

        if (tarjeta.Cuenta is null)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La tarjeta no tiene una cuenta asociada.");

        var cuentaPagadora = tarjeta.Cuenta;

        if (cuentaPagadora.IdEstado != 1)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("La cuenta no está activa.");

        if (cuentaPagadora.Saldo < dto.Monto)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("Fondos insuficientes.");

        cuentaPagadora.Debitar(dto.Monto);

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
            IdentificadorServicio = identificador,
            MontoTotalPagado = dto.Monto,
            MontoEmpresa95 = montoPrestadora,
            ComisionBanco5 = comisionBanco
        };

        await registrosPago.RegistrarAsync(registroPago, cancellationToken).ConfigureAwait(false);
        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo(
                "La transacción no pudo completarse porque el saldo fue modificado por otra operación simultánea. Por favor, verifique su saldo e intente de nuevo.");
        }

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
            identificador,
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

        var tipo = (TipoServicioPublico)tipoServicio;
        var identificadorLimpio = identificador.Trim();

        if (tipo == TipoServicioPublico.Telefonia)
        {
            if (!TelefoniaIdentificador.TryNormalizar(identificadorLimpio, out var digitos))
            {
                return ResultadoOperacion<decimal>.Fallo(
                    "El número telefónico debe tener entre 8 y 15 dígitos (solo dígitos; se permiten espacios, guiones o paréntesis como separadores).");
            }

            identificadorLimpio = digitos;
        }
        else if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(identificadorLimpio))
        {
            return ResultadoOperacion<decimal>.Fallo("El identificador no es válido.");
        }

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
        catch (NotSupportedException ex)
        {
            return ResultadoOperacion<decimal>.Fallo(ex.Message);
        }
    }
}
