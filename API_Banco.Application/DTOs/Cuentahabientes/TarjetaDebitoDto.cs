namespace API_Banco.Application.DTOs.Cuentahabientes;

/// <summary>
/// Datos de la tarjeta emitida para presentar al cliente.
/// </summary>
/// <remarks>
/// El PIN se entrega UNA SOLA VEZ aquí en texto claro (al momento de generarse).
/// El banco lo persiste hasheado; no hay forma de recuperarlo después.
/// </remarks>
public sealed record TarjetaDebitoDto(string NumeroTarjeta, int MesVencimiento, int AnioVencimiento, string Pin);
