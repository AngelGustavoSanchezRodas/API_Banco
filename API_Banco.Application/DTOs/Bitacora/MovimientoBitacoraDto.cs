namespace API_Banco.Application.DTOs.Bitacora;

/// <summary>
/// Movimiento cronológico de saldo (kardex) asociado a una cuenta.
/// </summary>
public sealed record MovimientoBitacoraDto(
    int IdTransaccion,
    int IdCuenta,
    decimal Monto,
    string CodigoTipoTransaccion,
    string? DescripcionTipoTransaccion,
    DateTime FechaUtc);
