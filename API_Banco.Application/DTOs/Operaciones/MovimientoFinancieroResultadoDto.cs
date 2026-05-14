namespace API_Banco.Application.DTOs.Operaciones;

/// <summary>
/// Resultado de un depósito o retiro con saldo posterior.
/// </summary>
public sealed record MovimientoFinancieroResultadoDto(
    int IdTransaccion,
    int IdCuenta,
    decimal Monto,
    decimal SaldoPosterior,
    DateTime FechaUtc);
