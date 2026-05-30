using API_Banco.Application.Persistencia;
using API_Banco.Domain.Entities;

namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Registro y consulta de movimientos para la bitácora / kardex.
/// </summary>
public interface ITransaccionRepositorio
{
    /// <summary>
    /// Crea un movimiento pendiente y devuelve la entidad rastreada por EF Core.
    /// El <see cref="TransaccionBanco.IdTransaccion"/> queda poblado automáticamente
    /// tras <see cref="IUnidadDeTrabajo.GuardarCambiosAsync"/>; no se requiere
    /// re-consultar el ID con queries auxiliares.
    /// </summary>
    Task<TransaccionBanco> CrearMovimientoPendienteAsync(
        int idCuenta,
        int idTipoTransaccion,
        decimal monto,
        DateTime fechaUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransaccionKardexItem>> ListarPorCuentaOrdenCronologicoAsync(
        int idCuenta,
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        CancellationToken cancellationToken = default);
}
