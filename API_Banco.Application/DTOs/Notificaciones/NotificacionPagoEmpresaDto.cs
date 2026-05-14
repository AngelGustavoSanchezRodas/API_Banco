using API_Banco.Application.DTOs.Pagos;

namespace API_Banco.Application.DTOs.Notificaciones;

/// <summary>
/// Carga enviada a la empresa para reflejar acreditación y saldo de servicio en línea.
/// </summary>
public sealed record NotificacionPagoEmpresaDto(
    TipoServicioPublico TipoServicio,
    string Identificador,
    decimal MontoAcreditado,
    string? ReferenciaCliente,
    string? ReferenciaTransaccionBanco,
    DateTime FechaUtc);
