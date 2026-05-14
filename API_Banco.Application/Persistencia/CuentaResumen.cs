namespace API_Banco.Application.Persistencia;

/// <summary>
/// Vista mínima de una cuenta para operaciones de aplicación.
/// </summary>
public sealed record CuentaResumen(int IdCuenta, string NoCuenta, decimal Saldo, int IdCliente, int IdEstado);
