using API_Banco.Application.Interfaces;

namespace API_Banco.Application.Services;

/// <summary>
/// Implementación por defecto de <see cref="IProveedorFecha"/> usando la hora del sistema en UTC.
/// </summary>
public sealed class ProveedorFechaSistema : IProveedorFecha
{
    /// <inheritdoc />
    public DateTime ObtenerUtcAhora() => DateTime.UtcNow;
}
