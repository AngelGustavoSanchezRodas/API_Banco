using API_Banco.Application.DTOs.Pagos;

namespace API_Banco.Application.Interfaces;

/// <summary>
/// Consulta de deuda en proveedores externos por tipo de servicio e identificador.
/// </summary>
public interface IConsultaDeudaServicio
{
    Task<decimal> ConsultarDeudaAsync(
        TipoServicioPublico tipoServicio,
        string identificador,
        CancellationToken cancellationToken = default);
}
