using API_Banco.Application.DTOs.Pagos;

namespace API_Banco.Application.Interfaces;

/// <summary>
/// Expone los identificadores de cuenta destino para la regla 95/5 (prestadora y cuenta corriente de comisiones del banco).
/// </summary>
public interface IConfiguracionDistribucionPagos
{
    Task<int> ObtenerIdCuentaPrestadoraAsync(TipoServicioPublico tipoServicio, CancellationToken cancellationToken = default);

    Task<int> ObtenerIdCuentaCorrienteComisionesBancoAsync(CancellationToken cancellationToken = default);
}
