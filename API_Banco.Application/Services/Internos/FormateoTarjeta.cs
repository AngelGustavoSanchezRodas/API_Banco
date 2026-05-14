namespace API_Banco.Application.Services.Internos;

/// <summary>
/// Formato de presentación de tarjeta sin exponer el PAN completo.
/// </summary>
internal static class FormateoTarjeta
{
    public static string Enmascarar(string numeroTarjeta)
    {
        if (string.IsNullOrWhiteSpace(numeroTarjeta) || numeroTarjeta.Length < 4)
            return "****";

        var ultimos = numeroTarjeta[^4..];
        return $"**** **** **** {ultimos}";
    }
}
