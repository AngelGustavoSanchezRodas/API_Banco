namespace API_Banco.Application.Interfaces;

/// <summary>
/// Generación de números de cuenta únicos (infraestructura: secuencia, GUID compacto, etc.).
/// </summary>
public interface INumeroCuentaGenerador
{
    Task<string> GenerarSiguienteNumeroCuentaAsync(CancellationToken cancellationToken = default);
}
