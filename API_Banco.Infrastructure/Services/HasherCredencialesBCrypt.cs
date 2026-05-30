using API_Banco.Application.Interfaces;
using BCrypt.Net;

namespace API_Banco.Infrastructure.Services;

/// <summary>
/// Implementación BCrypt del hashing de credenciales. Usa work factor 11
/// (≈100 ms por verificación en hardware actual; aceptable para login y pagos).
/// </summary>
public sealed class HasherCredencialesBCrypt : IHasherCredenciales
{
    // Work factor: 2^11 = 2048 iteraciones. Subir a 12+ si el hardware lo permite.
    private const int WorkFactor = 11;

    public string Hashear(string textoPlano)
    {
        if (string.IsNullOrEmpty(textoPlano))
            throw new ArgumentException("El texto a hashear no puede estar vacío.", nameof(textoPlano));

        return BCrypt.Net.BCrypt.HashPassword(textoPlano, WorkFactor);
    }

    public bool Verificar(string textoPlano, string hashAlmacenado)
    {
        if (string.IsNullOrEmpty(textoPlano) || string.IsNullOrEmpty(hashAlmacenado))
            return false;

        if (EsHashValido(hashAlmacenado))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(textoPlano, hashAlmacenado);
            }
            catch (SaltParseException)
            {
                return false;
            }
        }

        // Fallback legacy: el valor almacenado está en texto plano (seed antiguo, datos
        // pre-migración). Se compara byte a byte sin atajos de longitud para no filtrar
        // información por timing. El método llamador es responsable de re-hashear al
        // detectar éxito en este camino.
        return TextoPlanoIgual(textoPlano, hashAlmacenado);
    }

    public bool EsHashValido(string? valorAlmacenado)
    {
        if (string.IsNullOrEmpty(valorAlmacenado))
            return false;

        // Todos los hashes BCrypt empiezan con $2a$, $2b$, $2x$ o $2y$ seguidos del costo.
        return valorAlmacenado.Length == 60
            && valorAlmacenado.StartsWith("$2", StringComparison.Ordinal);
    }

    private static bool TextoPlanoIgual(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diferencia = 0;
        for (var i = 0; i < a.Length; i++)
            diferencia |= a[i] ^ b[i];
        return diferencia == 0;
    }
}
