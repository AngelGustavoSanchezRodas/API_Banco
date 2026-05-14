namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Resultado de la validación previa del identificador de servicio.
/// </summary>
public sealed record ValidacionIdentificadorResultadoDto(bool EsValido, string? Mensaje, string? ReferenciaExterna);
