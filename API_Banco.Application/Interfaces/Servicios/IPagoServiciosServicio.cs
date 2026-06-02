using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Pagos;

namespace API_Banco.Application.Interfaces.Servicios;

/// <summary>
/// Validación de identificadores y pagos de servicios con regla 95/5 y notificación en línea.
/// </summary>
public interface IPagoServiciosServicio
{
    Task<ResultadoOperacion<ValidacionIdentificadorResultadoDto>> ValidarIdentificadorAsync(
        ValidacionIdentificadorDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<PagoServicioResultadoDto>> EjecutarPagoServicioAsync(
        PagoServicioDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa un pago de servicios públicos recibido en ventanilla del banco
    /// (modalidad efectivo, sin tarjeta ni PIN). Se mantiene la regla 95/5,
    /// la validación del identificador y la notificación a la prestadora.
    /// </summary>
    /// <remarks>
    /// Solo debe ser invocado desde endpoints que ya hayan verificado el rol
    /// <c>ADMIN</c>; la regla de negocio asume que es un usuario del banco
    /// quien está recibiendo el efectivo.
    /// </remarks>
    Task<ResultadoOperacion<PagoVentanillaResultadoDto>> EjecutarPagoVentanillaAsync(
        PagoVentanillaDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<decimal>> ConsultarDeudaAsync(int tipoServicio, string identificador);
}
