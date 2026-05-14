namespace API_Banco.Application.Persistencia;

/// <summary>
/// Vista mínima de un cuentahabiente devuelta por la capa de persistencia (sin acoplar a entidades de dominio).
/// </summary>
public sealed record CuentahabienteResumen(int IdCliente, string Dpi, string Nombre, string Apellido);
