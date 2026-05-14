using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Bitacora;

namespace API_Banco.Application.Interfaces.Servicios;

/// <summary>
/// Bitácora cronológica (kardex) de movimientos por cuenta.
/// </summary>
public interface IBitacoraServicio
{
    Task<ResultadoOperacion<IReadOnlyList<MovimientoBitacoraDto>>> ObtenerKardexAsync(
        FiltroBitacoraDto filtro,
        CancellationToken cancellationToken = default);
}
