namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Respuesta de un reseteo de contraseña hecho por un administrador.
/// La <see cref="PasswordTemporal"/> es texto plano y se devuelve UNA sola vez:
/// el administrador debe transmitirla al cuentahabiente por un canal seguro y
/// éste la rehasheará a BCrypt al primer login exitoso.
/// </summary>
public sealed record PasswordReseteadaDto(
    int IdCliente,
    string NombreCompleto,
    string CorreoElectronico,
    string PasswordTemporal);
