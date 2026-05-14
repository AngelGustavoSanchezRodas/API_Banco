namespace API_Banco.Application.DTOs.Operaciones;

/// <summary>
/// Consulta de saldo disponible en tiempo real para una cuenta.
/// </summary>
public sealed record ConsultaSaldoDto(int IdCuenta, string NoCuenta, decimal SaldoDisponible, DateTime ConsultadoEnUtc);
