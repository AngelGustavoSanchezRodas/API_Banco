namespace API_Banco.Application.Services.Internos;

/// <summary>
/// Validaciones de entrada comunes (sin acceso a datos).
/// </summary>
internal static class ValidadoresEntrada
{
    public static bool EsDpiPlausible(string? dpi) =>
        !string.IsNullOrWhiteSpace(dpi) && dpi.Trim().Length >= 5;

    public static bool EsMontoValido(decimal monto) => monto > 0;

    public static bool EsIdentificadorServicioPlausible(string? identificador) =>
        !string.IsNullOrWhiteSpace(identificador) && identificador.Trim().Length >= 3;
}
