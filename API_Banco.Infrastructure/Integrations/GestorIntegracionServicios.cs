using System.Net.Http.Json;
using System.Text.Json;
using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;

namespace API_Banco.Infrastructure.Integrations;

/// <summary>
/// Adaptador HTTP único hacia las APIs externas (Universidad y Energía).
/// Implementa los tres puertos que usa Application: validar identificador,
/// consultar deuda y notificar pago acreditado.
/// </summary>
public sealed class GestorIntegracionServicios(IHttpClientFactory httpClientFactory)
    : IValidadorIdentificadorServicio, INotificacionEmpresaServicio, IConsultaDeudaServicio
{
    private const string ClienteUniversidad = "UniversidadApi";
    private const string ClienteEnergia = "EnergiaApi";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ResultadoValidacion> ValidarAsync(
        TipoServicioPublico tipoServicio,
        string identificador,
        CancellationToken cancellationToken = default)
    {
        switch (tipoServicio)
        {
            case TipoServicioPublico.Universidad:
            {
                var deuda = await ConsultarDeudaUniversidadAsync(identificador, cancellationToken).ConfigureAwait(false);
                return ResultadoValidacion.Valido(deuda.Carnet);
            }

            case TipoServicioPublico.EnergiaElectrica:
            {
                var deuda = await ConsultarDeudaEnergiaAsync(identificador, cancellationToken).ConfigureAwait(false);
                return ResultadoValidacion.Valido(deuda.NumeroContador);
            }

            case TipoServicioPublico.Telefonia:
                throw new NotSupportedException($"La integración para {tipoServicio} aún no está implementada.");
            default:
                throw new ArgumentOutOfRangeException(nameof(tipoServicio), tipoServicio, "Tipo de servicio no soportado.");
        }
    }

    public async Task<decimal> ConsultarDeudaAsync(
        TipoServicioPublico tipoServicio,
        string identificador,
        CancellationToken cancellationToken = default)
    {
        switch (tipoServicio)
        {
            case TipoServicioPublico.Universidad:
            {
                var deuda = await ConsultarDeudaUniversidadAsync(identificador, cancellationToken).ConfigureAwait(false);
                return deuda.MontoAdicional;
            }

            case TipoServicioPublico.EnergiaElectrica:
            {
                var deuda = await ConsultarDeudaEnergiaAsync(identificador, cancellationToken).ConfigureAwait(false);
                return deuda.SaldoPendiente;
            }

            case TipoServicioPublico.Telefonia:
                throw new NotSupportedException($"La integración para {tipoServicio} aún no está implementada.");
            default:
                throw new ArgumentOutOfRangeException(nameof(tipoServicio), tipoServicio, "Tipo de servicio no soportado.");
        }
    }

    public async Task NotificarPagoAcreditadoAsync(
        NotificacionPagoEmpresaDto notificacion,
        CancellationToken cancellationToken = default)
    {
        switch (notificacion.TipoServicio)
        {
            case TipoServicioPublico.Universidad:
            {
                var client = httpClientFactory.CreateClient(ClienteUniversidad);
                var request = new UniversidadPagoRequest(
                    notificacion.Identificador,
                    notificacion.MontoAcreditado,
                    notificacion.ReferenciaTransaccionBanco);

                using var response = await client
                    .PostAsJsonAsync("api/Universidad/pagos/confirmacion", request, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                await EnsureSuccessAsync(response, "Universidad", cancellationToken).ConfigureAwait(false);
                return;
            }

            case TipoServicioPublico.EnergiaElectrica:
            {
                var client = httpClientFactory.CreateClient(ClienteEnergia);
                var request = new EnergiaPagoRequest(
                    notificacion.Identificador,
                    notificacion.MontoAcreditado,
                    notificacion.ReferenciaTransaccionBanco);

                using var response = await client
                    .PostAsJsonAsync("api/IntegracionBancaria/pago", request, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                await EnsureSuccessAsync(response, "Energía", cancellationToken).ConfigureAwait(false);
                return;
            }

            case TipoServicioPublico.Telefonia:
                throw new NotSupportedException($"La integración para {notificacion.TipoServicio} aún no está implementada.");
            default:
                throw new ArgumentOutOfRangeException(nameof(notificacion.TipoServicio), notificacion.TipoServicio, "Tipo de servicio no soportado.");
        }
    }

    // ---------------- Universidad ----------------
    private async Task<UniversidadDeudaResponse> ConsultarDeudaUniversidadAsync(
        string identificador,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(ClienteUniversidad);
        using var response = await client
            .GetAsync($"api/Universidad/consultar/{Uri.EscapeDataString(identificador)}", cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, "Universidad", cancellationToken).ConfigureAwait(false);

        var payload = await response.Content
            .ReadFromJsonAsync<UniversidadDeudaResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload is null)
            throw new JsonException("La API de Universidad devolvió una respuesta vacía.");

        return payload;
    }

    // ---------------- Energía ----------------
    private async Task<EnergiaDeudaResponse> ConsultarDeudaEnergiaAsync(
        string identificador,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(ClienteEnergia);
        using var response = await client
            .GetAsync($"api/IntegracionBancaria/deuda/{Uri.EscapeDataString(identificador)}", cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, "Energía", cancellationToken).ConfigureAwait(false);

        var payload = await response.Content
            .ReadFromJsonAsync<EnergiaDeudaResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload is null)
            throw new JsonException("La API de Energía devolvió una respuesta vacía.");

        return payload;
    }

    // ---------------- Helpers ----------------
    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string proveedor,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new HttpRequestException(
            $"La API de {proveedor} respondió {(int)response.StatusCode} {response.ReasonPhrase}. Cuerpo: {body}");
    }
}
