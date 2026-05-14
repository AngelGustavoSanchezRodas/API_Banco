using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Cuentahabientes;

namespace API_Banco.Application.Interfaces.Servicios;

/// <summary>
/// Casos de uso de gestión de cuentahabientes: perfil, cuentas y tarjetas de débito.
/// </summary>
public interface ICuentahabienteServicio
{
    Task<ResultadoOperacion<CuentahabienteCreadoDto>> CrearPerfilAsync(
        CrearCuentahabienteDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<CuentaAbiertaDto>> AbrirCuentaConSaldoInicialAsync(
        AbrirCuentaDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<TarjetaDebitoDto>> AsociarTarjetaDebitoAsync(
        AsociarTarjetaDebitoDto dto,
        CancellationToken cancellationToken = default);
}
