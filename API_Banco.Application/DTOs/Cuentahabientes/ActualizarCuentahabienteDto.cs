using System.Text.Json.Serialization;

namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Campos editables del perfil de un cuentahabiente desde la consola de
/// administración. NO permite cambiar el DPI (es el identificador estable
/// del cliente) ni el tipo de cuenta (eso se hace al abrir cuentas nuevas).
///
/// Si se envía un correo distinto al actual, también se actualiza el
/// <c>correo_electronico</c> del <c>usuario_acceso</c> asociado para que el
/// canal de contacto y la dirección registrada en credenciales sigan
/// alineados.
/// </summary>
public sealed record ActualizarCuentahabienteDto(
    string Nombre,
    string Apellido,
    string Nit,
    [property: JsonPropertyName("telefono")] string? Celular,
    string? Email);
