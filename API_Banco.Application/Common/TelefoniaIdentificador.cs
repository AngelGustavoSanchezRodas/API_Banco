using System.Text;

namespace API_Banco.Application.Common;

/// <summary>
/// Normalización y validación básica del identificador de telefonía (solo dígitos, longitud razonable).
/// </summary>
public static class TelefoniaIdentificador
{
    /// <summary>
    /// Acepta dígitos con separadores opcionales (espacio, guión, paréntesis). Rechaza letras u otros símbolos.
    /// </summary>
    public static bool TryNormalizar(string? identificador, out string digitos)
    {
        digitos = string.Empty;
        if (string.IsNullOrWhiteSpace(identificador))
            return false;

        var sb = new StringBuilder(identificador.Trim().Length);
        foreach (var c in identificador.Trim())
        {
            if (char.IsAsciiDigit(c))
                sb.Append(c);
            else if (c is ' ' or '-' or '(' or ')')
                continue;
            else
                return false;
        }

        digitos = sb.ToString();
        return digitos.Length is >= 8 and <= 15;
    }
}
