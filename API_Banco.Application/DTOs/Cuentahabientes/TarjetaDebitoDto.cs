namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Datos de la tarjeta emitida para presentar al cliente.
/// </summary>
public sealed record TarjetaDebitoDto(string NumeroTarjeta, int MesVencimiento, int AnioVencimiento, string Cvv);
