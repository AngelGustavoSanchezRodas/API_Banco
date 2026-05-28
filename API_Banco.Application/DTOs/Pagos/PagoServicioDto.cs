namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Pago de servicios públicos/privados desde la cuenta del cuentahabiente aplicando la regla 95/5.
/// </summary>
/// <param name="NumeroTarjeta">16 dígitos de la tarjeta de débito.</param>
/// <param name="Pin">PIN numérico del cuentahabiente.</param>
/// <param name="TipoServicio">Tipo de servicio público pagado.</param>
/// <param name="Identificador">Identificador del cliente ante la empresa prestadora (NIS, carnet, etc.).</param>
/// <param name="Monto">Monto a pagar; debe coincidir con la deuda pendiente.</param>
/// <param name="ReferenciaCliente">Referencia libre que el cliente quiere asociar al pago.</param>
/// <param name="MesVencimiento">
/// Mes de vencimiento impreso en la tarjeta (1-12). Opcional por compatibilidad
/// con clientes antiguos: cuando el cliente lo envía, el banco lo valida contra
/// los datos almacenados de la tarjeta (junto con <paramref name="AnioVencimiento"/>).
/// </param>
/// <param name="AnioVencimiento">
/// Año de vencimiento impreso en la tarjeta (4 dígitos, ej. 2028). Opcional;
/// si se envía debe acompañarse de <paramref name="MesVencimiento"/>.
/// </param>
public sealed record PagoServicioDto(
    string NumeroTarjeta,
    string Pin,
    TipoServicioPublico TipoServicio,
    string Identificador,
    decimal Monto,
    string? ReferenciaCliente,
    int? MesVencimiento = null,
    int? AnioVencimiento = null);
