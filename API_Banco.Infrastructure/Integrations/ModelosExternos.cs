using System.Text.Json.Serialization;

namespace API_Banco.Infrastructure.Integrations;

// ----- Universidad -----
public record UniversidadPagoRequest(string Carnet, decimal Monto, string? ReferenciaBanco = null);
public record UniversidadDeudaResponse(string Carnet, decimal MontoAdicional);

// ----- Energía Eléctrica -----
public record EnergiaPagoRequest(string NumeroContador, decimal Monto, string? ReferenciaBanco = null);
public record EnergiaDeudaResponse(string NumeroContador, decimal SaldoPendiente);

// ----- Telefonía -----
public record TelefoniaPagoRequest(string NumeroTelefonico, decimal Monto, string? ReferenciaBanco = null);
public record TelefoniaDeudaResponse(
    [property: JsonPropertyName("numero_telefonico")] string NumeroTelefonico,
    [property: JsonPropertyName("deuda_pendiente")] decimal DeudaPendiente,
    [property: JsonPropertyName("total_pagar")] decimal TotalPagar,
    [property: JsonPropertyName("id_factura")] int? IdFactura);
