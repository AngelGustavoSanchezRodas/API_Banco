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
}
