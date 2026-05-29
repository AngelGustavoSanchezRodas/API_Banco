namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Cuenta bancaria de un cuentahabiente para listados en el panel.
/// </summary>
public sealed record CuentaListadaDto(
    int IdCuenta,
    string NoCuenta,
    decimal Saldo,
    int IdTipoCuenta,
    string? DescripcionTipoCuenta,
    int IdEstado,
    string? NumeroTarjeta = null,
    int? MesVencimiento = null,
    int? AnioVencimiento = null);
