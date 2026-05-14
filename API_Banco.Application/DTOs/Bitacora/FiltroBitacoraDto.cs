namespace API_Banco.Application.DTOs.Bitacora;

/// <summary>
/// Filtro para consultar la bitácora tipo kardex de una cuenta en orden cronológico.
/// </summary>
public sealed record FiltroBitacoraDto(int IdCuenta, DateTime? DesdeUtc, DateTime? HastaUtc);
