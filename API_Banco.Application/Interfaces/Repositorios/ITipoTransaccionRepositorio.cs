namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Resolución de identificadores de tipo de transacción definidos en <see cref="Constants.CodigosTipoTransaccion"/>.
/// </summary>
public interface ITipoTransaccionRepositorio
{
    Task<int?> ObtenerIdPorCodigoDescripcionAsync(string codigoDescripcion, CancellationToken cancellationToken = default);
}
