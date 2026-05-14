namespace API_Banco.Application.Common;

/// <summary>
/// Resultado de la validación de un identificador de servicio (universidad, telefonía, energía).
/// </summary>
public sealed class ResultadoValidacion
{
    private ResultadoValidacion(bool esValido, string? mensaje, string? referenciaExterna)
    {
        EsValido = esValido;
        Mensaje = mensaje;
        ReferenciaExterna = referenciaExterna;
    }

    public bool EsValido { get; }

    public string? Mensaje { get; }

    /// <summary>
    /// Referencia opcional devuelta por sistemas externos (p. ej. id de abonado en la empresa).
    /// </summary>
    public string? ReferenciaExterna { get; }

    public static ResultadoValidacion Valido(string? referenciaExterna = null) =>
        new(true, null, referenciaExterna);

    public static ResultadoValidacion Invalido(string mensaje) => new(false, mensaje, null);
}
