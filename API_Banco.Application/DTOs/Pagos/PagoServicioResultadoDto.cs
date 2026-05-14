namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Resultado del pago con montos distribuidos y saldos relevantes.
/// </summary>
public sealed record PagoServicioResultadoDto(
    int IdTransaccionDebitoCuentahabiente,
    int IdTransaccionAcreditacionPrestadora,
    int IdTransaccionComisionBanco,
    decimal MontoTotal,
    decimal MontoAcreditadoPrestadora,
    decimal ComisionBanco,
    decimal SaldoPosteriorCuentahabiente,
    DateTime FechaUtc,
    bool NotificacionEnviada);
