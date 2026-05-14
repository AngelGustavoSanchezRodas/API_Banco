namespace API_Banco.Application.Interfaces;

/// <summary>
/// Generación de número de tarjeta de débito (infraestructura: algoritmo interno, BIN, etc.).
/// </summary>
public interface INumeroTarjetaGenerador
{
    Task<string> GenerarSiguienteNumeroTarjetaAsync(CancellationToken cancellationToken = default);
}
