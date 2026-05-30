namespace API_Banco.Application.DTOs.Operaciones;

/// <summary>
/// Respuesta tras suspender o reactivar una cuenta bancaria.
/// </summary>
public sealed record CambioEstadoCuentaDto(
    int IdCuenta,
    string NoCuenta,
    int IdEstadoAnterior,
    int IdEstadoNuevo,
    string DescripcionEstadoNuevo,
    int TarjetasAfectadas,
    DateTime FechaUtc);
