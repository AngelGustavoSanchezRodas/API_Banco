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

    Task<ResultadoOperacion<decimal>> ConsultarDeudaAsync(int tipoServicio, string identificador);
}
