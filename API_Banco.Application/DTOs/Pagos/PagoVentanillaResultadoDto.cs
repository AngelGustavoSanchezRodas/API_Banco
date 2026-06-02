namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Comprobante devuelto al admin que registró un pago en ventanilla. Lleva
/// los IDs de las cuatro transacciones generadas para que se puedan rastrear
/// individualmente en la bitácora si más adelante hace falta auditarlas.
/// </summary>
/// <param name="IdTransaccionIngresoEfectivo">+monto en la cuenta interna de comisiones (caja).</param>
/// <param name="IdTransaccionEgresoPrestadora">-95% desde la cuenta de comisiones hacia la prestadora.</param>
/// <param name="IdTransaccionAcreditacionPrestadora">+95% acreditado a la prestadora.</param>
/// <param name="IdTransaccionComisionBanco">+5% acreditado a la cuenta de comisiones del banco.</param>
/// <param name="MontoTotal">Monto cobrado en efectivo al pagador.</param>
/// <param name="MontoAcreditadoPrestadora">95% del monto total.</param>
/// <param name="ComisionBanco">5% del monto total.</param>
/// <param name="FechaUtc">Marca temporal UTC del pago.</param>
/// <param name="NotificacionEnviada">
/// <c>true</c> si la empresa prestadora confirmó la recepción de la notificación
/// del pago. Si es <c>false</c>, el pago igual quedó confirmado en el banco
/// y debe conciliarse manualmente con la empresa.
/// </param>
public sealed record PagoVentanillaResultadoDto(
    int IdTransaccionIngresoEfectivo,
    int IdTransaccionEgresoPrestadora,
    int IdTransaccionAcreditacionPrestadora,
    int IdTransaccionComisionBanco,
    decimal MontoTotal,
    decimal MontoAcreditadoPrestadora,
    decimal ComisionBanco,
    DateTime FechaUtc,
    bool NotificacionEnviada);
