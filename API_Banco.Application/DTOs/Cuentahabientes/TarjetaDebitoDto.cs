namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Datos de la tarjeta emitida (sin exponer el PIN completo en logs; el valor se devuelve solo en alta).
/// </summary>
public sealed record TarjetaDebitoDto(int IdTarjeta, string NumeroTarjetaEnmascarado, int IdCuenta, DateTime FechaVencimiento);
