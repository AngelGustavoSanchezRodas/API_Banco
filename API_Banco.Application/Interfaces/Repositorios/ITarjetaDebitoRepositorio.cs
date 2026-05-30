using API_Banco.Application.Persistencia;
using API_Banco.Domain.Entities;

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

    Task<TarjetaDebito?> ObtenerPorNumeroAsync(string numeroTarjeta, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca como INACTIVAS todas las tarjetas que actualmente estén ACTIVAS en la cuenta indicada.
    /// Se usa al reemitir una tarjeta para garantizar la regla de negocio
    /// "una sola tarjeta activa por cuenta".
    /// </summary>
    /// <returns>Cantidad de tarjetas que fueron bloqueadas.</returns>
    Task<int> BloquearTarjetasActivasDeCuentaAsync(
        int idCuenta,
        int idEstadoActivo,
        int idEstadoInactivo,
        CancellationToken cancellationToken = default);
}
