using API_Banco.Application.Persistencia;

namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Persistencia de clientes (cuentahabientes). La infraestructura mapea hacia las entidades de dominio.
/// </summary>
public interface IClienteRepositorio
{
    Task<bool> ExisteDpiAsync(string dpi, CancellationToken cancellationToken = default);

    Task<CuentahabienteResumen?> ObtenerPorIdAsync(int idCliente, CancellationToken cancellationToken = default);

    Task<CuentahabienteResumen?> ObtenerPorDpiAsync(string dpi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un nuevo cliente pendiente de confirmación con <see cref="IUnidadDeTrabajo.GuardarCambiosAsync"/>.
    /// </summary>
    Task RegistrarPendienteAsync(
        string dpi,
        string nombre,
        string apellido,
        string? celular,
        string? email,
        CancellationToken cancellationToken = default);
}
