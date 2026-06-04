namespace API_Banco.Application.Services.Internos;

/// <summary>
/// Validaciones de entrada comunes (sin acceso a datos).
/// </summary>
internal static class ValidadoresEntrada
{
    /// <summary>
    /// Tope máximo permitido en una sola operación de ingreso de efectivo
    /// (depósito por ventanilla o activación de cuenta con saldo inicial).
    ///
    /// Se aplica como blindaje contra errores de captura/abuso: para
    /// operaciones más grandes el banco debe escalar al área de cumplimiento
    /// y registrarlas por un flujo manual con doble validación.
    /// </summary>
    public const decimal MontoMaximoOperacion = 50_000m;

    public static bool EsDpiPlausible(string? dpi) =>
        !string.IsNullOrWhiteSpace(dpi) && dpi.Trim().Length >= 5;

    public static bool EsMontoValido(decimal monto) => monto > 0;

    /// <summary>
    /// True si el monto está dentro del rango permitido para una sola
    /// operación de ingreso de efectivo (mayor que 0 y menor o igual al
    /// tope <see cref="MontoMaximoOperacion"/>).
    /// </summary>
    public static bool EstaDentroDelTopeOperacion(decimal monto) =>
        monto > 0 && monto <= MontoMaximoOperacion;

    public static bool EsIdentificadorServicioPlausible(string? identificador) =>
        !string.IsNullOrWhiteSpace(identificador) && identificador.Trim().Length >= 3;
}
