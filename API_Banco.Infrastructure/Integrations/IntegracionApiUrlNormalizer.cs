namespace API_Banco.Infrastructure.Integrations;

/// <summary>
/// Normaliza la URL base de APIs externas para evitar rutas duplicadas en HttpClient.
/// </summary>
public static class IntegracionApiUrlNormalizer
{
    /// <summary>
    /// Deja solo el origen (esquema + host). Quita sufijos como <c>/api/Telefonia</c> si se configuraron por error.
    /// </summary>
    public static string NormalizarBaseTelefonia(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed;

        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.Equals("/api/Telefonia", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/api/Telefonia", StringComparison.OrdinalIgnoreCase))
        {
            return uri.GetLeftPart(UriPartial.Authority);
        }

        return trimmed;
    }
}
