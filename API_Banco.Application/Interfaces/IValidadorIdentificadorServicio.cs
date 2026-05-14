using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Pagos;

namespace API_Banco.Application.Interfaces;

/// <summary>
/// Valida identificadores únicos contra sistemas de universidad, telefonía o energía eléctrica (implementación en infraestructura).
/// </summary>
public interface IValidadorIdentificadorServicio
{
    Task<ResultadoValidacion> ValidarAsync(
        TipoServicioPublico tipoServicio,
        string identificador,
        CancellationToken cancellationToken = default);
}
