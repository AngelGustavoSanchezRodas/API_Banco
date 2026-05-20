using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace API_Banco.Infrastructure.Integrations;

public sealed record EnergiaPagoRequest(string Identificador, string NumeroContador, decimal Monto);

public sealed class GestorIntegracionServiciosHTTP(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
    : IValidadorIdentificadorServicio, INotificacionEmpresaServicio, IConsultaDeudaServicio
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Uri _energiaBaseUri = ResolverBaseEnergiaUri(configuration);

    public async Task<ResultadoValidacion> ValidarAsync(
        TipoServicioPublico tipoServicio,
        string identificador,
        CancellationToken cancellationToken = default)
    {
        if (tipoServicio != TipoServicioPublico.EnergiaElectrica)
            throw new NotSupportedException($"La integración para {tipoServicio} aún no está implementada.");

        _ = await ConsultarDeudaEnergiaAsync(identificador, cancellationToken).ConfigureAwait(false);
        return ResultadoValidacion.Valido(identificador);
    }

    public async Task<decimal> ConsultarDeudaAsync(
        TipoServicioPublico tipoServicio,
        string identificador,
        CancellationToken cancellationToken = default)
    {
        if (tipoServicio != TipoServicioPublico.EnergiaElectrica)
            throw new NotSupportedException($"La integración para {tipoServicio} aún no está implementada.");

        return await ConsultarDeudaEnergiaAsync(identificador, cancellationToken).ConfigureAwait(false);
    }

    public async Task NotificarPagoAcreditadoAsync(
        NotificacionPagoEmpresaDto notificacion,
        CancellationToken cancellationToken = default)
    {
        if (notificacion.TipoServicio != TipoServicioPublico.EnergiaElectrica)
            throw new NotSupportedException($"La integración para {notificacion.TipoServicio} aún no está implementada.");

        var client = httpClientFactory.CreateClient();
        var request = new EnergiaPagoRequest(
            notificacion.Identificador,
            notificacion.Identificador,
            notificacion.MontoAcreditado);

        using var response = await client
            .PostAsJsonAsync(BuildEnergiaUri("api/IntegracionBancaria/pago"), request, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is null or > 0)
        {
            _ = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<decimal> ConsultarDeudaEnergiaAsync(
        string identificador,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var response = await client
            .GetAsync(BuildEnergiaUri($"api/IntegracionBancaria/deuda/{Uri.EscapeDataString(identificador)}"), cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new JsonException("La API de Energía devolvió una respuesta vacía.");

        if (payload.ValueKind == JsonValueKind.Number && payload.TryGetDecimal(out var valorDirecto))
            return valorDirecto;

        if (payload.ValueKind == JsonValueKind.Object &&
            TryGetDecimal(payload, out var valor, "saldoPendiente", "monto", "deuda", "montoAdicional", "saldo"))
        {
            return valor;
        }

        throw new JsonException("La API de Energía devolvió un formato de deuda inesperado.");
    }

    private Uri BuildEnergiaUri(string relativePath) => new(_energiaBaseUri, relativePath);

    private static Uri ResolverBaseEnergiaUri(IConfiguration configuration)
    {
        var energiaApiUrl = configuration["Integraciones:EnergiaApiUrl"];
        if (string.IsNullOrWhiteSpace(energiaApiUrl))
            throw new InvalidOperationException("Falta configurar Integraciones:EnergiaApiUrl.");

        if (!Uri.TryCreate(energiaApiUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("Integraciones:EnergiaApiUrl debe ser una URL absoluta.");

        return baseUri;
    }

    private static bool TryGetDecimal(JsonElement element, out decimal valor, params string[] propiedades)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!propiedades.Any(p => string.Equals(p, property.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (property.Value.ValueKind == JsonValueKind.Number &&
                property.Value.TryGetDecimal(out valor))
            {
                return true;
            }

            if (property.Value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(property.Value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out valor))
            {
                return true;
            }
        }

        valor = default;
        return false;
    }
}
