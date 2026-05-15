namespace API_Banco.Infrastructure.Integrations;

public record UniversidadPagoRequest(string Carnet, decimal Monto);

public record UniversidadDeudaResponse(string Carnet, decimal MontoAdicional);
