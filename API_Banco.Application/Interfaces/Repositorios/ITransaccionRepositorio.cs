using API_Banco.Application.Persistencia;

namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Registro y consulta de movimientos para la bitácora / kardex.
/// </summary>
public interface ITransaccionRepositorio
{
    /// <summary>
    /// Registra un movimiento pendiente de confirmación con <see cref="IUnidadDeTrabajo.GuardarCambiosAsync"/>.
    /// </summary>
    Task RegistrarMovimientoPendienteAsync(
        int idCuenta,
        int idTipoTransaccion,
        decimal monto,
        DateTime fechaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el identificador del último movimiento que coincide con los datos registrados (tras guardar cambios).
    /// </summary>
    Task<int> ObtenerIdUltimaTransaccionAsync(
        int idCuenta,
        DateTime fechaUtc,
        decimal monto,
        int idTipoTransaccion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransaccionKardexItem>> ListarPorCuentaOrdenCronologicoAsync(
        int idCuenta,
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        CancellationToken cancellationToken = default);
}
