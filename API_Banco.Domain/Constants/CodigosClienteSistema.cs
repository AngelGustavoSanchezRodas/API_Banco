namespace API_Banco.Application.Constants;

/// <summary>
/// Identificadores del "cliente sistema" — registro técnico que es titular de las
/// cuentas internas del banco (comisiones y recaudación de servicios). No es un
/// cuentahabiente real: no debe aparecer en el padrón ni recibir operaciones de cliente
/// (reset de password, emisión de tarjeta, suspensión, etc.).
/// </summary>
public static class CodigosClienteSistema
{
    /// <summary>DPI sembrado en <c>schema/wipe_and_admin.sql</c> para identificar al cliente sistema.</summary>
    public const string Dpi = "0000000000100";

    /// <summary>NIT sembrado para el cliente sistema.</summary>
    public const string Nit = "CF-SISTEMA";
}
