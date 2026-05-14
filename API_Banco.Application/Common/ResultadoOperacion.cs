namespace API_Banco.Application.Common;

/// <summary>
/// Resultado genérico de operaciones de aplicación sin excepciones de control de flujo.
/// </summary>
/// <typeparam name="T">Tipo del valor en caso de éxito.</typeparam>
public sealed class ResultadoOperacion<T>
{
    private ResultadoOperacion(bool exito, T? valor, string? mensajeError, IReadOnlyList<string>? detalles)
    {
        Exito = exito;
        Valor = valor;
        MensajeError = mensajeError;
        Detalles = detalles ?? Array.Empty<string>();
    }

    public bool Exito { get; }

    public T? Valor { get; }

    public string? MensajeError { get; }

    public IReadOnlyList<string> Detalles { get; }

    public static ResultadoOperacion<T> Ok(T valor) => new(true, valor, null, null);

    public static ResultadoOperacion<T> Fallo(string mensaje, params string[] detalles) =>
        new(false, default, mensaje, detalles);
}

/// <summary>
/// Variante sin carga útil para comandos que solo indican éxito o error.
/// </summary>
public sealed class ResultadoOperacion
{
    private ResultadoOperacion(bool exito, string? mensajeError, IReadOnlyList<string>? detalles)
    {
        Exito = exito;
        MensajeError = mensajeError;
        Detalles = detalles ?? Array.Empty<string>();
    }

    public bool Exito { get; }

    public string? MensajeError { get; }

    public IReadOnlyList<string> Detalles { get; }

    public static ResultadoOperacion Ok() => new(true, null, null);

    public static ResultadoOperacion Fallo(string mensaje, params string[] detalles) =>
        new(false, mensaje, detalles);
}
