namespace API_Banco.Application.DTOs.Admin;

/// <summary>
/// Métricas agregadas para el dashboard del rol ADMIN.
/// Todas las cifras se calculan con queries agregadas en la BD
/// (no se traen filas individuales al servidor de aplicación).
/// </summary>
/// <param name="ClientesRegistrados">Cantidad total de cuentahabientes en el padrón.</param>
/// <param name="OperacionesHoy">
/// Movimientos "primarios" registrados en la bitácora durante el día UTC actual.
/// Se cuentan únicamente DEPOSITO, RETIRO, TRANSFERENCIA_ORIGEN y
/// PAGO_SERVICIO_DEBITO_CUENTAHABIENTE, para no inflar el dato con las contrapartidas.
/// </param>
/// <param name="VolumenMensual">
/// Suma del monto de las transacciones "primarias" del mes UTC en curso.
/// Misma lista de tipos que <see cref="OperacionesHoy"/>.
/// </param>
/// <param name="CuentasInactivas">
/// Cantidad de cuentas bancarias en estado INACTIVO (suspendidas por ADMIN).
/// </param>
/// <param name="GeneradoUtc">Marca temporal UTC del cálculo.</param>
public sealed record MetricasAdminDto(
    int ClientesRegistrados,
    int OperacionesHoy,
    decimal VolumenMensual,
    int CuentasInactivas,
    DateTime GeneradoUtc);
