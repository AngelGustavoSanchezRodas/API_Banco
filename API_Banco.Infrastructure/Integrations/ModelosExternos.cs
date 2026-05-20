namespace API_Banco.Infrastructure.Integrations;

public record UniversidadPagoRequest(string Carnet, decimal Monto, string? ReferenciaBanco = null);

public record UniversidadDeudaResponse(string Carnet, decimal MontoAdicional);
