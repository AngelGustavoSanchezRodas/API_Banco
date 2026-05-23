namespace API_Banco.Infrastructure.Integrations;

// ----- Universidad -----
public record UniversidadPagoRequest(string Carnet, decimal Monto, string? ReferenciaBanco = null);
public record UniversidadDeudaResponse(string Carnet, decimal MontoAdicional);

// ----- Energía Eléctrica -----
public record EnergiaPagoRequest(string NumeroContador, decimal Monto, string? ReferenciaBanco = null);
public record EnergiaDeudaResponse(string NumeroContador, decimal SaldoPendiente);
