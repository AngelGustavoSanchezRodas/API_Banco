namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Pago de servicios públicos/privados desde la cuenta del cuentahabiente aplicando la regla 95/5.
/// </summary>
public sealed record PagoServicioDto(
    int IdCuentaPagadora,
    TipoServicioPublico TipoServicio,
    string Identificador,
    decimal Monto,
    string? ReferenciaCliente);
