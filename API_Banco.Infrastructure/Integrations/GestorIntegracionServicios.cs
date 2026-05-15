using System.Net.Http.Json;
using System.Text.Json;
using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;

namespace API_Banco.Infrastructure.Integrations;

public sealed class GestorIntegracionServicios(IHttpClientFactory httpClientFactory)
    : IValidadorIdentificadorServicio, INotificacionEmpresaServicio, IConsultaDeudaServicio
{
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

            case TipoServicioPublico.Telefonia:
            case TipoServicioPublico.EnergiaElectrica:
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

            case TipoServicioPublico.Telefonia:
            case TipoServicioPublico.EnergiaElectrica:
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
                var client = httpClientFactory.CreateClient("UniversidadApi");
                var request = new UniversidadPagoRequest(notificacion.Identificador, notificacion.MontoAcreditado);

                using var response = await client
                    .PostAsJsonAsync("api/Universidad/pagar", request, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength is null or > 0)
                {
                    _ = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            case TipoServicioPublico.Telefonia:
            case TipoServicioPublico.EnergiaElectrica:
                throw new NotSupportedException($"La integración para {notificacion.TipoServicio} aún no está implementada.");
            default:
                throw new ArgumentOutOfRangeException(nameof(notificacion.TipoServicio), notificacion.TipoServicio, "Tipo de servicio no soportado.");
        }
    }

    private async Task<UniversidadDeudaResponse> ConsultarDeudaUniversidadAsync(
        string identificador,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("UniversidadApi");
        using var response = await client
            .GetAsync($"api/Universidad/consultar/{Uri.EscapeDataString(identificador)}", cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<UniversidadDeudaResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload is null)
            throw new JsonException("La API de Universidad devolvió una respuesta vacía.");

        return payload;
    }
}
