using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Cuentahabientes;
using API_Banco.Application.Persistencia;
using API_Banco.Domain.Entities;

namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Acceso a cuentas monetarias y actualización atómica de saldos.
/// </summary>
public interface ICuentaRepositorio
{
    Task<CuentaResumen?> ObtenerPorIdAsync(int idCuenta, CancellationToken cancellationToken = default);

    Task<CuentaResumen?> ObtenerPorNumeroAsync(string noCuenta, CancellationToken cancellationToken = default);

    Task<Cuenta?> ObtenerEntidadPorIdAsync(int idCuenta, CancellationToken cancellationToken = default);

    Task<bool> PerteneceAClienteAsync(int idCuenta, int idCliente, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CuentaListadaDto>> ListarPorClienteAsync(
        int idCliente,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las cuentas internas operacionales del banco
    /// (tipo <see cref="CodigosTipoCuenta.CuentaInternaBanco"/>): comisiones y
    /// cuentas de recaudación de prestadoras de servicios.
    /// </summary>
    Task<IReadOnlyList<CuentaInternaDto>> ListarCuentasInternasAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si la cuenta corresponde al tipo <see cref="CodigosTipoCuenta.CuentaInternaBanco"/>.
    /// Útil para defensa en profundidad antes de aplicar operaciones administrativas
    /// (suspensión/reactivación, emisión de tarjeta, etc.) que no aplican a cuentas internas.
    /// </summary>
    Task<bool> EsCuentaInternaAsync(int idCuenta, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra una cuenta pendiente de confirmación con <see cref="IUnidadDeTrabajo.GuardarCambiosAsync"/>.
    /// </summary>
    Task RegistrarCuentaPendienteAsync(
        string noCuenta,
        Cliente cliente,
        int idTipoCuenta,
        int idEstado,
        decimal saldoInicial,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica un delta al saldo (positivo abona, negativo retira). Debe fallar sin modificar si el saldo no alcanza en retiros.
    /// </summary>
    Task<bool> IntentarAplicarDeltaSaldoAsync(int idCuenta, decimal delta, CancellationToken cancellationToken = default);
}
