namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Asociación de una tarjeta de débito a una cuenta existente del cuentahabiente.
/// </summary>
public sealed record AsociarTarjetaDebitoDto(int IdCliente, int IdCuenta, string Pin, DateTime FechaVencimiento);
