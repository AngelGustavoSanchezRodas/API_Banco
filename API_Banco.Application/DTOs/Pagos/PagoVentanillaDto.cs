namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Pago de servicios públicos efectuado en la ventanilla del banco. Simula a
/// un cliente que llega físicamente con efectivo: no se debita ninguna cuenta
/// del banco; el dinero entra como efectivo y se distribuye automáticamente
/// con la regla 95/5 (95% a la cuenta de la empresa prestadora y 5% a la
/// cuenta interna de comisiones del banco).
/// </summary>
/// <param name="TipoServicio">Tipo de servicio público pagado.</param>
/// <param name="Identificador">Identificador del cliente ante la empresa prestadora (NIS, carnet, teléfono, etc.).</param>
/// <param name="Monto">Monto entregado en efectivo; debe coincidir con la deuda pendiente cuando aplica.</param>
/// <param name="ReferenciaCliente">Referencia libre que se anota al pago (ej. "Pago efectivo - Juan Pérez").</param>
/// <param name="NombrePagador">
/// Nombre opcional de la persona que llega a pagar en ventanilla. No es validado
/// contra ningún registro del banco; se almacena en la referencia para el comprobante.
/// </param>
/// <param name="DocumentoPagador">
/// DPI/NIT opcional del pagador para incluir en el comprobante. No se valida.
/// </param>
public sealed record PagoVentanillaDto(
    TipoServicioPublico TipoServicio,
    string Identificador,
    decimal Monto,
    string? ReferenciaCliente,
    string? NombrePagador = null,
    string? DocumentoPagador = null);
