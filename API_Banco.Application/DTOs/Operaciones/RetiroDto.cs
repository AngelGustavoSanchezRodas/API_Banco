namespace API_Banco.Application.DTOs.Operaciones;

/// <summary>
/// Retiro de fondos validando disponibilidad de saldo en tiempo real.
/// </summary>
public sealed record RetiroDto(int IdCuenta, decimal Monto, string? Referencia);
