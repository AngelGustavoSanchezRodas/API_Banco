using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace API_Banco.Infrastructure.Services;

public class ValidadorIdentificadorMock : IValidadorIdentificadorServicio
{
    public Task<ResultadoValidacion> ValidarAsync(TipoServicioPublico tipoServicio, string identificador, CancellationToken cancellationToken = default)
    {
        // Simulamos que cualquier identificador válido es aceptado por la Universidad/Empresa Eléctrica
        return Task.FromResult(ResultadoValidacion.Valido("REF-EXT-" + new Random().Next(1000, 9999)));
    }
}

public class NotificacionEmpresaMock : INotificacionEmpresaServicio
{
    public Task NotificarPagoAcreditadoAsync(NotificacionPagoEmpresaDto notificacion, CancellationToken cancellationToken = default)
    {
        // Aquí iría el HttpClient (fetch) hacia la API de la Universidad o Luz
        Console.WriteLine($"[API EXTERNA] Notificando a {notificacion.TipoServicio} el pago de Q{notificacion.MontoAcreditado}");
        return Task.CompletedTask;
    }
}

public class GeneradoresMock : INumeroCuentaGenerador, INumeroTarjetaGenerador
{
    public Task<string> GenerarSiguienteNumeroCuentaAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Random().Next(10000000, 99999999).ToString()); // 8 dígitos aleatorios
    }

    public Task<string> GenerarSiguienteNumeroTarjetaAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("4123" + new Random().Next(10000000, 99999999).ToString() + "1234"); // Visa simulada
    }
}

public class ConfiguracionPagosMock(IConfiguration configuration) : IConfiguracionDistribucionPagos
{
    public Task<int> ObtenerIdCuentaPrestadoraAsync(TipoServicioPublico tipoServicio, CancellationToken cancellationToken = default)
    {
        var clave = $"Pagos:CuentasPrestadoras:{tipoServicio}";
        var valor = configuration.GetValue<int?>(clave)
            ?? configuration.GetValue<int?>("Pagos:IdCuentaPrestadoraPorDefecto")
            ?? throw new InvalidOperationException($"Falta configurar {clave} o Pagos:IdCuentaPrestadoraPorDefecto.");
        return Task.FromResult(valor);
    }

    public Task<int> ObtenerIdCuentaCorrienteComisionesBancoAsync(CancellationToken cancellationToken = default)
    {
        var valor = configuration.GetValue<int?>("Pagos:IdCuentaComisiones")
            ?? throw new InvalidOperationException("Falta configurar Pagos:IdCuentaComisiones.");
        return Task.FromResult(valor);
    }
}