using Microsoft.AspNetCore.Authentication;

namespace API_Banco.Auth;

/// <summary>
/// Opciones del esquema de autenticación por API Key (header <c>X-Api-Key</c>).
/// Usado por las APIs externas (Universidad, Energía, Telefonía) cuando invocan
/// endpoints del banco que no van detrás de un JWT de usuario final.
/// </summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}
