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

    /// <summary>
    /// Actualiza los datos editables de un cuentahabiente existente: nombre,
    /// apellido, NIT, celular y correo. El DPI permanece inmutable porque es
    /// el identificador estable del cliente y está referenciado por sus
    /// usuarios de acceso (nombre_usuario = DPI).
    ///
    /// Si el correo cambia, también se sincroniza con el
    /// <c>correo_electronico</c> del <c>usuario_acceso</c> asociado para
    /// mantener consistencia entre el padrón y las credenciales.
    /// </summary>
    Task<ResultadoOperacion<CuentahabienteActualizadoDto>> ActualizarPerfilAsync(
        int idCliente,
        ActualizarCuentahabienteDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<CuentaAbiertaDto>> AbrirCuentaConSaldoInicialAsync(
        AbrirCuentaDto dto,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<TarjetaDebitoDto>> AsociarTarjetaDebitoAsync(
        AsociarTarjetaDebitoDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resetea la contraseña del usuario asociado al cuentahabiente.
    /// Genera una nueva password temporal criptográficamente segura, la hashea
    /// con BCrypt y devuelve el texto plano al admin UNA sola vez para que se
    /// la entregue al cliente por canal seguro.
    /// Operación restringida a usuarios con rol CLIENTE (no aplica a ADMIN).
    /// </summary>
    Task<ResultadoOperacion<PasswordReseteadaDto>> ResetearPasswordAsync(
        int idCliente,
        CancellationToken cancellationToken = default);
}
