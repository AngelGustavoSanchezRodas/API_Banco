using API_Banco.Application.Persistencia;

namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Persistencia de tarjetas de débito asociadas a cuentas.
/// </summary>
public interface ITarjetaDebitoRepositorio
{
    /// <summary>
    /// Registra la tarjeta pendiente de confirmación con <see cref="IUnidadDeTrabajo.GuardarCambiosAsync"/>.
    /// </summary>
    Task RegistrarTarjetaPendienteAsync(
        int idCuenta,
        string numeroTarjeta,
        string pin,
        DateTime fechaVencimiento,
        int idEstado,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la tarjeta más reciente asociada a la cuenta (tras guardar cambios).
    /// </summary>
    Task<TarjetaDebitoCreada?> ObtenerUltimaPorCuentaAsync(int idCuenta, CancellationToken cancellationToken = default);
}
