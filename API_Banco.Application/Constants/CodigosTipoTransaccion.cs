namespace API_Banco.Application.Constants;

/// <summary>
/// Descripciones/códigos lógicos de tipos de transacción. La infraestructura debe mapearlos a <c>IdTipoTransaccion</c> en base de datos.
/// </summary>
public static class CodigosTipoTransaccion
{
    public const string Deposito = "DEPOSITO";
    public const string Retiro = "RETIRO";
    public const string PagoServicioDebitoCuentahabiente = "PAGO_SERVICIO_DEBITO_CUENTAHABIENTE";
    public const string PagoServicioAcreditacionPrestadora = "PAGO_SERVICIO_ACREDITACION_PRESTADORA";
    public const string PagoServicioComisionBanco = "PAGO_SERVICIO_COMISION_BANCO";
}
