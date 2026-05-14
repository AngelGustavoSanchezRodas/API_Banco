namespace API_Banco.Application.Interfaces;

/// <summary>
/// Resuelve identificadores de estado lógicos (activo/inactivo) hacia claves de base de datos.
/// </summary>
public interface IEstadoRepositorio
{
    Task<int?> ObtenerIdPorCodigoAsync(string codigoEstado, CancellationToken cancellationToken = default);
}
