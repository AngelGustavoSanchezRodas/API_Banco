namespace API_Banco.Application.Interfaces;

/// <summary>
/// Hashing y verificación de credenciales sensibles (contraseñas de usuario y PIN de tarjeta).
/// La implementación debe usar un algoritmo lento y con sal por hash (BCrypt, Argon2, PBKDF2).
/// </summary>
public interface IHasherCredenciales
{
    /// <summary>
    /// Genera un hash criptográfico (con sal interna) del texto plano recibido.
    /// </summary>
    string Hashear(string textoPlano);

    /// <summary>
    /// Compara el texto plano contra el hash almacenado.
    /// Si el hash almacenado no tiene formato reconocible (datos legacy en texto plano),
    /// la implementación puede fallback a comparación exacta para no romper datos existentes.
    /// </summary>
    bool Verificar(string textoPlano, string hashAlmacenado);

    /// <summary>
    /// Indica si el valor almacenado YA tiene formato de hash criptográfico válido.
    /// Útil para detectar credenciales legacy en texto plano y migrarlas
    /// de forma transparente al primer uso exitoso.
    /// </summary>
    bool EsHashValido(string? valorAlmacenado);
}
