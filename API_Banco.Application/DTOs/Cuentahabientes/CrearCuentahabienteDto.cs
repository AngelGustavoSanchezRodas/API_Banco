using System.Text.Json.Serialization;

namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Datos para registrar el perfil de un cuentahabiente (persona natural).
/// </summary>
public sealed record CrearCuentahabienteDto(
    string Dpi,
    string Nombre,
    string Apellido,
    [property: JsonPropertyName("telefono")] string? Celular,
    string? Email,
    int IdTipoCuenta);
