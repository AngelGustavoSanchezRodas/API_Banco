using Microsoft.AspNetCore.Mvc;

namespace API_Banco.Controllers
{
    /// <summary>
    /// Endpoints SOLO de diagnóstico de configuración en runtime.
    /// Permiten verificar a qué URLs externas está apuntando este App Service
    /// y si las API keys requeridas están presentes — sin exponer su valor.
    /// </summary>
    /// <remarks>
    /// Útil porque en Azure App Service las "Application Settings" sobrescriben
    /// los valores de appsettings.json en runtime, y normalmente la única forma
    /// de saber qué quedó efectivamente cargado es revisar el portal.
    /// Este endpoint permite hacerlo sin entrar al portal.
    /// </remarks>
    [ApiController]
    [Route("api/diagnostico")]
    public class DiagnosticoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostEnvironment _environment;

        public DiagnosticoController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IHostEnvironment environment)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        /// <summary>
        /// Devuelve qué URL base y qué API key (ofuscada) está usando el banco
        /// para hablar con Universidad y con Energía.
        /// </summary>
        [HttpGet("integraciones")]
        public IActionResult Integraciones()
        {
            // Resolvemos tanto la URL "cruda" leída de IConfiguration como la
            // BaseAddress real del HttpClient nombrado, porque ahí es donde se
            // ve si Azure App Settings está sobrescribiendo el JSON.
            var universidadConfig = _configuration["Integraciones:UniversidadApiUrl"];
            var energiaConfig = _configuration["Integraciones:EnergiaApiUrl"];
            var energiaApiKey = _configuration["Integraciones:EnergiaApiKey"];

            string? universidadBase = null;
            string? energiaBase = null;
            bool energiaTieneApiKeyEnHeader = false;

            try
            {
                var clienteUni = _httpClientFactory.CreateClient("UniversidadApi");
                universidadBase = clienteUni.BaseAddress?.ToString();
            }
            catch
            {
                universidadBase = "(no se pudo crear el HttpClient)";
            }

            try
            {
                var clienteEnergia = _httpClientFactory.CreateClient("EnergiaApi");
                energiaBase = clienteEnergia.BaseAddress?.ToString();
                energiaTieneApiKeyEnHeader = clienteEnergia.DefaultRequestHeaders.Contains("X-Api-Key");
            }
            catch
            {
                energiaBase = "(no se pudo crear el HttpClient)";
            }

            return Ok(new
            {
                ambiente = _environment.EnvironmentName,
                universidad = new
                {
                    urlConfigurada = universidadConfig ?? "(no configurada)",
                    urlEnHttpClient = universidadBase ?? "(no resuelta)"
                },
                energia = new
                {
                    urlConfigurada = energiaConfig ?? "(no configurada)",
                    urlEnHttpClient = energiaBase ?? "(no resuelta)",
                    apiKeyPresente = !string.IsNullOrWhiteSpace(energiaApiKey),
                    apiKeyEnHeader = energiaTieneApiKeyEnHeader,
                    apiKeyHuella = OfuscarKey(energiaApiKey)
                }
            });
        }

        /// <summary>
        /// Devuelve los primeros 3 y últimos 3 caracteres de la key con el
        /// resto enmascarado. Suficiente para comparar visualmente que dos
        /// despliegues tienen la misma key sin exponer el secreto.
        /// </summary>
        private static string OfuscarKey(string? key)
        {
            if (string.IsNullOrEmpty(key)) return "(vacía)";
            if (key.Length <= 6) return new string('*', key.Length);
            return $"{key[..3]}***{key[^3..]} (largo={key.Length})";
        }
    }
}
