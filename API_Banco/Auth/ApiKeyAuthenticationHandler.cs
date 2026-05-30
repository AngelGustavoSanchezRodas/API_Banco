using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace API_Banco.Auth;

/// <summary>
/// Valida el header <c>X-Api-Key</c> contra la lista de keys configuradas en
/// <c>Pagos:ApiKeysSocios</c>. Cada key identifica a un socio bancario (universidad,
/// energía, telefonía, etc.) y se emite con un nombre lógico para auditoría.
/// </summary>
/// <remarks>
/// Formato de configuración esperado en <c>appsettings.json</c>:
/// <code>
/// "Pagos": {
///   "ApiKeysSocios": {
///     "Universidad": "REEMPLAZAR_EN_AZURE_KEY_VAULT",
///     "Energia": "REEMPLAZAR_EN_AZURE_KEY_VAULT",
///     "Telefonia": "REEMPLAZAR_EN_AZURE_KEY_VAULT"
///   }
/// }
/// </code>
/// Cuando una key matchea, se emite un <see cref="ClaimsPrincipal"/> con el rol
/// <c>SOCIO_BANCARIO</c> y un claim <c>socio</c> con el nombre del emisor.
/// </remarks>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var keyValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var apiKey = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        var keysSocios = _configuration.GetSection("Pagos:ApiKeysSocios").GetChildren();
        foreach (var entry in keysSocios)
        {
            var keyConfigurada = entry.Value;
            if (string.IsNullOrWhiteSpace(keyConfigurada))
                continue;

            // Comparación constant-time para no filtrar prefijos vía timing attack.
            if (KeyComparator.Igual(apiKey, keyConfigurada))
            {
                var identity = new ClaimsIdentity(ApiKeyAuthenticationOptions.Scheme);
                identity.AddClaim(new Claim("socio", entry.Key));
                identity.AddClaim(new Claim(ClaimTypes.Role, "SOCIO_BANCARIO"));
                identity.AddClaim(new Claim(ClaimTypes.Name, entry.Key));

                var ticket = new AuthenticationTicket(
                    new ClaimsPrincipal(identity),
                    ApiKeyAuthenticationOptions.Scheme);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        return Task.FromResult(AuthenticateResult.Fail("API Key inválida."));
    }

    private static class KeyComparator
    {
        public static bool Igual(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
