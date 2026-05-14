namespace API_Banco.Application.DTOs.Operaciones;

/// <summary>
/// Abono a una cuenta existente.
/// </summary>
public sealed record DepositoDto(int IdCuenta, decimal Monto, string? Referencia);
