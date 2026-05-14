namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Solicitud de apertura de cuenta monetaria con saldo inicial.
/// </summary>
public sealed record AbrirCuentaDto(int IdCliente, decimal SaldoInicial);
