using API_Banco.Application.Persistencia;

namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Acceso a cuentas monetarias y actualización atómica de saldos.
/// </summary>
public interface ICuentaRepositorio
{
    Task<CuentaResumen?> ObtenerPorIdAsync(int idCuenta, CancellationToken cancellationToken = default);

    Task<CuentaResumen?> ObtenerPorNumeroAsync(string noCuenta, CancellationToken cancellationToken = default);

    Task<bool> PerteneceAClienteAsync(int idCuenta, int idCliente, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra una cuenta pendiente de confirmación con <see cref="IUnidadDeTrabajo.GuardarCambiosAsync"/>.
    /// </summary>
    Task RegistrarCuentaPendienteAsync(
        string noCuenta,
        decimal saldoInicial,
        int idCliente,
        int idEstado,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica un delta al saldo (positivo abona, negativo retira). Debe fallar sin modificar si el saldo no alcanza en retiros.
    /// </summary>
    Task<bool> IntentarAplicarDeltaSaldoAsync(int idCuenta, decimal delta, CancellationToken cancellationToken = default);
}
