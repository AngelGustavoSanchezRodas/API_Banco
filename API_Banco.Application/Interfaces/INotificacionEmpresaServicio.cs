using API_Banco.Application.DTOs.Notificaciones;

namespace API_Banco.Application.Interfaces;

/// <summary>
/// Notificación en línea a la empresa luego de un pago exitoso (HTTP, cola, etc. en infraestructura).
/// </summary>
public interface INotificacionEmpresaServicio
{
    /// <summary>
    /// Informa a la empresa para reflejar saldos y dejar el servicio al día; debe ser invocado tras persistir el pago.
    /// </summary>
    Task NotificarPagoAcreditadoAsync(NotificacionPagoEmpresaDto notificacion, CancellationToken cancellationToken = default);
}
