namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Respuesta tras crear el perfil del cuentahabiente.
/// </summary>
public sealed record CuentahabienteCreadoDto(int IdCliente, string Dpi, string NombreCompleto);
