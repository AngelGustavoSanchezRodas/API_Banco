namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Cuenta interna operacional del banco (comisiones, recaudación de servicios, etc.).
/// No pertenece a un cuentahabiente real; se administra por separado del padrón.
/// </summary>
public sealed record CuentaInternaDto(
    int IdCuenta,
    string NoCuenta,
    decimal SaldoActual,
    int IdTipoCuenta,
    string? DescripcionTipoCuenta,
    int IdEstado,
    string? DescripcionEstado);
