using System.Net.Http.Json;
using System.Text.Json;
using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace API_Banco.Infrastructure.Integrations;

/// <summary>
/// Adaptador HTTP hacia las APIs externas (Universidad, Energía y opcionalmente Telefonía).
/// Telefonía usa catálogo de demostración en configuración (<c>Integraciones:TelefoniaDemoPostpago</c>)
/// cuando no hay integración HTTP activa.
/// </summary>
public sealed class GestorIntegracionServicios(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GestorIntegracionServicios> logger)
    : IValidadorIdentificadorServicio, INotificacionEmpresaServicio, IConsultaDeudaServicio
{
    private const string ClienteUniversidad = "UniversidadApi";
    private const string ClienteEnergia = "EnergiaApi";
    private const string ClienteTelefonia = "TelefoniaApi";

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
            {
                if (!TelefoniaIdentificador.TryNormalizar(identificador, out var digitos))
                {
                    return ResultadoValidacion.Invalido(
                        "El número telefónico debe tener entre 8 y 15 dígitos (solo dígitos; se permiten espacios, guiones o paréntesis como separadores).");
                }

                return ResultadoValidacion.Valido(digitos);
            }

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
            {
                if (!TelefoniaIdentificador.TryNormalizar(identificador, out var digitos))
                    return 0m;

                return ObtenerDeudaTelefoniaDesdeConfiguracion(digitos);
            }

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
            {
                if (!TelefoniaHttpEstaConfigurado())
                {
                    logger.LogInformation(
                    "Telefonía: sin URL de integración activa; el cobro quedó registrado en el banco. Tel={Tel}, monto={Monto}, refBanco={Ref}",
                    notificacion.Identificador,
                    notificacion.MontoAcreditado,
                    notificacion.ReferenciaTransaccionBanco);
                    return;
                }

                var client = httpClientFactory.CreateClient(ClienteTelefonia);
                var request = new TelefoniaPagoRequest(
                    notificacion.Identificador,
                    notificacion.MontoAcreditado,
                    notificacion.ReferenciaTransaccionBanco);

                var rutaRelativa = configuration["Integraciones:TelefoniaNotificacionRutaRelativa"]?.Trim().TrimStart('/')
                    ?? "api/IntegracionBancaria/pago";

                using var response = await client
                    .PostAsJsonAsync(rutaRelativa, request, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                await EnsureSuccessAsync(response, "Telefonía", cancellationToken).ConfigureAwait(false);
                return;
            }

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

    // ---------------- Telefonía (demostración + callback HTTP opcional) ----------------
    private decimal ObtenerDeudaTelefoniaDesdeConfiguracion(string digitos)
    {
        var valorTexto = configuration[$"Integraciones:TelefoniaDemoPostpago:{digitos}"];
        if (string.IsNullOrWhiteSpace(valorTexto))
            return 0m;

        if (!decimal.TryParse(valorTexto, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var deuda))
            return 0m;

        return deuda < 0m ? 0m : deuda;
    }

    private bool TelefoniaHttpEstaConfigurado()
    {
        var url = configuration["Integraciones:TelefoniaApiUrl"]?.Trim();
        return !string.IsNullOrWhiteSpace(url)
            && !url.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(url, UriKind.Absolute, out _);
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
