namespace API_Banco.Application.Constants;

/// <summary>
/// Códigos lógicos de tipos de cuenta (campo <c>descripcion</c> en la tabla <c>tipo_cuenta</c>).
/// La infraestructura resuelve el <c>IdTipoCuenta</c> correspondiente.
/// </summary>
public static class CodigosTipoCuenta
{
    public const string Ahorro = "AHORRO";
    public const string Corriente = "CORRIENTE";
    public const string CuentaInternaBanco = "CUENTA_INTERNA_BANCO";
}
