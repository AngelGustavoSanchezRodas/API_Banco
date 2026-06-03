namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Vista del cuentahabiente justo después de aplicar una actualización de perfil.
/// Coincide con la forma de <c>CuentahabienteResumen</c> para que el frontend
/// pueda reemplazar la fila correspondiente en sus listados sin transformaciones.
/// </summary>
public sealed record CuentahabienteActualizadoDto(
    int IdCliente,
    string Dpi,
    string Nombre,
    string Apellido,
    string Nit,
    string? Celular,
    string? Email);
