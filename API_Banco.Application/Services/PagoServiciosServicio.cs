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
    IEstadoRepositorio estados,
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

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        if (idEstadoActivo is null)
            return ResultadoOperacion<PagoServicioResultadoDto>.Fallo("Estado ACTIVO no configurado.");

        var tarjeta = await tarjetas.ObtenerPorNumeroAsync(dto.NumeroTarjeta.Trim(), cancellationToken).ConfigureAwait(false);
        if (tarjeta is null || tarjeta.IdEstado != idEstadoActivo.Value)
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

        if (cuentaPagadora.IdEstado != idEstadoActivo.Value)
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
        var transaccionPrestadora = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaPrestadora, idTipoPrestadora.Value, montoPrestadora, ahora, cancellationToken)
            .ConfigureAwait(false);
        var transaccionComision = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaComisiones, idTipoComision.Value, comisionBanco, ahora, cancellationToken)
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

        // EF Core poblado IdTransaccion en cada entidad tras el SaveChanges anterior.
        var idDebito = transaccionDebito.IdTransaccion;
        var idPrestadora = transaccionPrestadora.IdTransaccion;
        var idComision = transaccionComision.IdTransaccion;

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
    public async Task<ResultadoOperacion<PagoVentanillaResultadoDto>> EjecutarPagoVentanillaAsync(
        PagoVentanillaDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1) Validaciones de entrada (idénticas al flujo del cliente para coherencia).
        var identificador = dto.Identificador?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(identificador))
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("El identificador del servicio es obligatorio.");

        if (dto.TipoServicio == TipoServicioPublico.Telefonia)
        {
            if (!TelefoniaIdentificador.TryNormalizar(identificador, out var digitos))
            {
                return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                    "El número telefónico debe tener entre 8 y 15 dígitos (solo dígitos; se permiten espacios, guiones o paréntesis como separadores).");
            }
            identificador = digitos;
        }
        else if (!ValidadoresEntrada.EsIdentificadorServicioPlausible(identificador))
        {
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("El identificador no es válido.");
        }

        if (!ValidadoresEntrada.EsMontoValido(dto.Monto))
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("El monto del pago debe ser mayor que cero.");

        // 2) Validar identificador con la empresa prestadora.
        ResultadoValidacion validacion;
        try
        {
            validacion = await validadorIdentificador
                .ValidarAsync(dto.TipoServicio, identificador, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                "No se pudo validar el identificador en el proveedor externo.",
                ex.Message);
        }
        catch (JsonException ex)
        {
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                "La respuesta del proveedor externo no tiene el formato esperado.",
                ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(ex.Message);
        }

        if (!validacion.EsValido)
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                validacion.Mensaje ?? "No se pudo validar el identificador ante la empresa.");

        if (dto.TipoServicio == TipoServicioPublico.Telefonia && !string.IsNullOrWhiteSpace(validacion.ReferenciaExterna))
            identificador = validacion.ReferenciaExterna;

        // 3) Consultar deuda y validar el monto entregado.
        decimal deudaPendiente;
        try
        {
            deudaPendiente = await consultaDeudaServicio
                .ConsultarDeudaAsync(dto.TipoServicio, identificador, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                "No se pudo consultar la deuda pendiente del servicio.",
                ex.Message);
        }

        if (dto.TipoServicio == TipoServicioPublico.Telefonia)
        {
            if (deudaPendiente > 0 && dto.Monto != deudaPendiente)
                return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                    $"El monto debe coincidir con la deuda pendiente (Q{deudaPendiente:N2}).");
        }
        else
        {
            if (deudaPendiente <= 0)
                return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("El servicio no tiene deuda pendiente.");
            if (dto.Monto != deudaPendiente)
                return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                    $"El monto debe coincidir con la deuda pendiente (Q{deudaPendiente:N2}).");
        }

        // 4) Resolver tipos de transacción y cuentas internas.
        var idTipoIngresoEfectivo = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.PagoVentanillaIngresoEfectivo, cancellationToken)
            .ConfigureAwait(false);
        var idTipoEgresoPrestadora = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.PagoVentanillaTransferenciaPrestadora, cancellationToken)
            .ConfigureAwait(false);
        var idTipoPrestadora = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.PagoServicioAcreditacionPrestadora, cancellationToken)
            .ConfigureAwait(false);
        var idTipoComision = await tiposTransaccion
            .ObtenerIdPorCodigoDescripcionAsync(CodigosTipoTransaccion.PagoServicioComisionBanco, cancellationToken)
            .ConfigureAwait(false);

        if (idTipoIngresoEfectivo is null || idTipoEgresoPrestadora is null ||
            idTipoPrestadora is null || idTipoComision is null)
        {
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                "Faltan tipos de transacción configurados para pagos en ventanilla. " +
                "Aplica el parche schema/add_tipos_pago_ventanilla.sql en la base de datos.");
        }

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
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                "No se pudo resolver la configuración de cuentas para la distribución del pago.",
                ex.Message);
        }

        if (idCuentaPrestadora == idCuentaComisiones)
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                "La cuenta prestadora no puede coincidir con la cuenta de comisiones.");

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        if (idEstadoActivo is null)
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("Estado ACTIVO no configurado.");

        var cuentaPrestadora = await cuentas.ObtenerEntidadPorIdAsync(idCuentaPrestadora, cancellationToken).ConfigureAwait(false);
        if (cuentaPrestadora is null)
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("La cuenta de la empresa prestadora no existe.");
        if (cuentaPrestadora.IdEstado != idEstadoActivo.Value)
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("La cuenta de la empresa prestadora no está activa.");

        var cuentaComisiones = await cuentas.ObtenerEntidadPorIdAsync(idCuentaComisiones, cancellationToken).ConfigureAwait(false);
        if (cuentaComisiones is null)
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("La cuenta de comisiones del banco no existe.");
        if (cuentaComisiones.IdEstado != idEstadoActivo.Value)
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo("La cuenta de comisiones del banco no está activa.");

        // 5) Distribución contable balanceada en la cuenta de comisiones (rol caja):
        //
        //   cuenta_comisiones :  +monto     (ingreso efectivo)
        //   cuenta_comisiones :  -monto95   (transfer a prestadora)
        //   cuenta_prestadora :  +monto95   (acreditación 95%)
        //   cuenta_comisiones :  +comision5 (comisión banco 5%)
        //
        //   Saldo neto cuenta_comisiones = +comision5   (igual que flujo cliente)
        //   Saldo neto cuenta_prestadora = +monto95
        //
        // Se ejecutan en el orden lógico contable, pero todas en la misma unidad
        // de trabajo (un único SaveChanges) para mantener atomicidad.
        var (montoPrestadora, comisionBanco) = DistribuidorPago95Por5.Calcular(dto.Monto);
        var ahora = fecha.ObtenerUtcAhora();

        cuentaComisiones.Acreditar(dto.Monto);          // entra efectivo
        cuentaComisiones.Debitar(montoPrestadora);      // sale a prestadora
        cuentaPrestadora.Acreditar(montoPrestadora);    // entra a prestadora
        cuentaComisiones.Acreditar(comisionBanco);      // queda comisión

        var transaccionIngreso = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaComisiones, idTipoIngresoEfectivo.Value, dto.Monto, ahora, cancellationToken)
            .ConfigureAwait(false);
        var transaccionEgresoPrestadora = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaComisiones, idTipoEgresoPrestadora.Value, montoPrestadora, ahora, cancellationToken)
            .ConfigureAwait(false);
        var transaccionAcreditacionPrestadora = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaPrestadora, idTipoPrestadora.Value, montoPrestadora, ahora, cancellationToken)
            .ConfigureAwait(false);
        var transaccionComision = await transacciones
            .CrearMovimientoPendienteAsync(idCuentaComisiones, idTipoComision.Value, comisionBanco, ahora, cancellationToken)
            .ConfigureAwait(false);

        // 6) Registrar el pago de servicio. Apuntamos el "origen" a la transacción
        //    de ingreso de efectivo, que es la primera del pipeline y representa
        //    el evento de negocio "el banco recibió el dinero del cliente".
        //
        //    La referencia almacenada incluye el nombre/documento del pagador
        //    cuando vienen, para tener trazabilidad de la persona física que pagó
        //    en caja. La referencia libre del operador (si existe) se concatena.
        var partesReferencia = new List<string> { "VENTANILLA" };
        if (!string.IsNullOrWhiteSpace(dto.NombrePagador))
            partesReferencia.Add(dto.NombrePagador!.Trim());
        if (!string.IsNullOrWhiteSpace(dto.DocumentoPagador))
            partesReferencia.Add(dto.DocumentoPagador!.Trim());
        if (!string.IsNullOrWhiteSpace(dto.ReferenciaCliente))
            partesReferencia.Add(dto.ReferenciaCliente!.Trim());
        var referenciaUnificada = string.Join(" | ", partesReferencia);

        var registroPago = new RegistroPagoServicio
        {
            TransaccionOrigen = transaccionIngreso,
            EntidadServicio = CodigosEntidadServicio.ParaRegistro(dto.TipoServicio),
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
            return ResultadoOperacion<PagoVentanillaResultadoDto>.Fallo(
                "La transacción no pudo completarse porque otra operación modificó las cuentas en paralelo. Intenta de nuevo.");
        }

        // 7) Notificar a la empresa prestadora. Si falla, el pago ya está
        //    confirmado en BD; igual respondemos OK con notificacionEnviada=false
        //    para que el operador sepa que debe conciliar manualmente.
        var notificacion = new NotificacionPagoEmpresaDto(
            dto.TipoServicio,
            identificador,
            dto.Monto,
            referenciaUnificada,
            transaccionIngreso.IdTransaccion.ToString(),
            ahora);

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
                "Notificación VENTANILLA falló | tipoServicio={Tipo} identificador={Identificador} monto={Monto} referencia={Referencia}. " +
                "El cobro en caja ya está confirmado; se requiere conciliación manual con la prestadora.",
                notificacion.TipoServicio,
                notificacion.Identificador,
                notificacion.MontoAcreditado,
                notificacion.ReferenciaTransaccionBanco);
        }

        var resultado = new PagoVentanillaResultadoDto(
            IdTransaccionIngresoEfectivo: transaccionIngreso.IdTransaccion,
            IdTransaccionEgresoPrestadora: transaccionEgresoPrestadora.IdTransaccion,
            IdTransaccionAcreditacionPrestadora: transaccionAcreditacionPrestadora.IdTransaccion,
            IdTransaccionComisionBanco: transaccionComision.IdTransaccion,
            MontoTotal: dto.Monto,
            MontoAcreditadoPrestadora: montoPrestadora,
            ComisionBanco: comisionBanco,
            FechaUtc: ahora,
            NotificacionEnviada: notificacionEnviada);

        return ResultadoOperacion<PagoVentanillaResultadoDto>.Ok(resultado);
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
