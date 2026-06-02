using API_Banco.Application.DTOs.Admin;

namespace API_Banco.Application.Interfaces.Servicios;

/// <summary>
/// Calcula métricas agregadas para el dashboard del rol ADMIN.
/// </summary>
public interface IAdminMetricasServicio
{
    Task<MetricasAdminDto> ObtenerMetricasAsync(CancellationToken cancellationToken = default);
}
