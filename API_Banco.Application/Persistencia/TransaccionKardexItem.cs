namespace API_Banco.Application.Persistencia;

/// <summary>
/// Elemento de kardex listado desde persistencia (incluye código lógico del tipo de transacción).
/// </summary>
public sealed record TransaccionKardexItem(
    int IdTransaccion,
    int IdCuenta,
    decimal Monto,
    DateTime FechaUtc,
    string CodigoTipoTransaccion,
    string? DescripcionTipoTransaccion);
