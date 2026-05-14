namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Respuesta de apertura de cuenta con identificador bancario y saldo disponible.
/// </summary>
public sealed record CuentaAbiertaDto(int IdCuenta, string NoCuenta, decimal Saldo, int IdCliente);
