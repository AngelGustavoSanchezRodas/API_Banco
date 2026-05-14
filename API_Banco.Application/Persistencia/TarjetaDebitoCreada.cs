namespace API_Banco.Application.Persistencia;

/// <summary>
/// Datos de una tarjeta recién persistida.
/// </summary>
public sealed record TarjetaDebitoCreada(
    int IdTarjeta,
    string NumeroTarjeta,
    int IdCuenta,
    DateTime FechaVencimiento);
