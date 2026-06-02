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
    public const string TransferenciaOrigen = "TRANSFERENCIA_ORIGEN";
    public const string TransferenciaDestino = "TRANSFERENCIA_DESTINO";

    /// <summary>
    /// Ingreso de efectivo recibido en ventanilla del banco para pagar un servicio
    /// público. Se acredita a la cuenta interna de comisiones (rol de caja),
    /// dejando rastro contable de la entrada del dinero físico al banco. Es la
    /// transacción <c>origen</c> del <c>RegistroPagoServicio</c> en pagos en ventanilla.
    /// </summary>
    public const string PagoVentanillaIngresoEfectivo = "PAGO_VENTANILLA_INGRESO_EFECTIVO";

    /// <summary>
    /// Egreso desde la cuenta interna de comisiones (rol de caja) hacia la cuenta
    /// de la empresa prestadora cuando un cliente paga su servicio en la
    /// ventanilla del banco con efectivo. Compensa la acreditación del 95% en
    /// la cuenta prestadora para mantener el balance contable.
    /// </summary>
    public const string PagoVentanillaTransferenciaPrestadora = "PAGO_VENTANILLA_TRANSFERENCIA_PRESTADORA";
}
