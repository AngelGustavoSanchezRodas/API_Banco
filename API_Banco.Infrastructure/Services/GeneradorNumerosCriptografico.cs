using System.Security.Cryptography;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using Microsoft.Extensions.Configuration;

namespace API_Banco.Infrastructure.Services;

/// <summary>
/// Generador de números de cuenta y tarjeta con RNG criptográfico y verificación
/// de unicidad contra base de datos. Reintenta hasta <see cref="MaxIntentos"/> veces
/// antes de lanzar, lo que en la práctica nunca debería ocurrir dado el espacio
/// de valores (10^8 para cuenta, 10^8 para tarjeta).
/// </summary>
/// <remarks>
/// El generador anterior basado en <c>new Random()</c> tomaba el tick de reloj
/// como semilla. Bajo carga concurrente (varias creaciones de tarjeta en milisegundos
/// adyacentes) era posible obtener semillas iguales y colisionar contra el índice
/// único de <c>no_cuenta</c> / <c>no_tarjeta</c>, levantando un
/// <c>DbUpdateException</c> sin reintento.
/// </remarks>
public sealed class GeneradorNumerosCriptografico(
    ICuentaRepositorio cuentaRepositorio,
    ITarjetaDebitoRepositorio tarjetaRepositorio)
    : INumeroCuentaGenerador, INumeroTarjetaGenerador
{
    private const int MaxIntentos = 10;
    private const string PrefijoTarjetaVisa = "4123";
    private const string SufijoTarjetaDemo = "1234";

    public async Task<string> GenerarSiguienteNumeroCuentaAsync(CancellationToken cancellationToken = default)
    {
        for (var intento = 0; intento < MaxIntentos; intento++)
        {
            var candidato = RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString();
            var existente = await cuentaRepositorio
                .ObtenerPorNumeroAsync(candidato, cancellationToken)
                .ConfigureAwait(false);

            if (existente is null)
                return candidato;
        }

        throw new InvalidOperationException(
            $"No se pudo generar un número de cuenta único tras {MaxIntentos} intentos. Revise el espacio de IDs disponibles.");
    }

    public async Task<string> GenerarSiguienteNumeroTarjetaAsync(CancellationToken cancellationToken = default)
    {
        for (var intento = 0; intento < MaxIntentos; intento++)
        {
            var medio = RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString();
            var candidato = $"{PrefijoTarjetaVisa}{medio}{SufijoTarjetaDemo}";

            var existente = await tarjetaRepositorio
                .ObtenerPorNumeroAsync(candidato, cancellationToken)
                .ConfigureAwait(false);

            if (existente is null)
                return candidato;
        }

        throw new InvalidOperationException(
            $"No se pudo generar un número de tarjeta único tras {MaxIntentos} intentos.");
    }
}

/// <summary>
/// Resuelve los IDs de cuenta prestadora y cuenta de comisiones desde <see cref="IConfiguration"/>
/// (sección <c>Pagos</c>). Permite cambiar cuentas internas sin tocar código.
/// </summary>
public sealed class ConfiguracionPagosPorAppSettings(IConfiguration configuration)
    : IConfiguracionDistribucionPagos
{
    public Task<int> ObtenerIdCuentaPrestadoraAsync(
        Application.DTOs.Pagos.TipoServicioPublico tipoServicio,
        CancellationToken cancellationToken = default)
    {
        var clave = $"Pagos:CuentasPrestadoras:{tipoServicio}";
        var valor = configuration.GetValue<int?>(clave)
            ?? configuration.GetValue<int?>("Pagos:IdCuentaPrestadoraPorDefecto")
            ?? throw new InvalidOperationException(
                $"Falta configurar {clave} o Pagos:IdCuentaPrestadoraPorDefecto.");
        return Task.FromResult(valor);
    }

    public Task<int> ObtenerIdCuentaCorrienteComisionesBancoAsync(CancellationToken cancellationToken = default)
    {
        var valor = configuration.GetValue<int?>("Pagos:IdCuentaComisiones")
            ?? throw new InvalidOperationException("Falta configurar Pagos:IdCuentaComisiones.");
        return Task.FromResult(valor);
    }
}
