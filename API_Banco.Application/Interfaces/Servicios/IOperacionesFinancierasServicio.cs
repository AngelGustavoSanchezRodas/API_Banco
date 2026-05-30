using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Operaciones;

namespace API_Banco.Application.Interfaces.Servicios;

/// <summary>
/// Depósitos, retiros y consulta de saldo en tiempo real.
/// </summary>
public interface IOperacionesFinancierasServicio
{
    Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> DepositarAsync(
        DepositoDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> RetirarAsync(
        RetiroDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<ConsultaSaldoDto>> ConsultarSaldoDisponibleAsync(
        int idCuenta,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> ActivarCuentaConDepositoAsync(
        int idCuenta,
        decimal montoDeposito);

    Task<ResultadoOperacion<MovimientoFinancieroResultadoDto>> TransferirAsync(
        int idCuentaOrigen,
        int idCuentaDestino,
        decimal monto,
        string descripcion);

    /// <summary>
    /// Suspende una cuenta ACTIVA: pasa a INACTIVA y todas sus tarjetas activas
    /// quedan bloqueadas (INACTIVAS). Operación reservada al administrador.
    /// La cuenta sigue pudiendo recibir transferencias (no se pierde el dinero).
    /// </summary>
    Task<ResultadoOperacion<CambioEstadoCuentaDto>> SuspenderCuentaAsync(
        int idCuenta,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactiva una cuenta previamente suspendida (INACTIVA → ACTIVA).
    /// No re-emite tarjeta automáticamente: el admin debe emitir una nueva tras reactivar.
    /// </summary>
    Task<ResultadoOperacion<CambioEstadoCuentaDto>> ReactivarCuentaAsync(
        int idCuenta,
        CancellationToken cancellationToken = default);
}
