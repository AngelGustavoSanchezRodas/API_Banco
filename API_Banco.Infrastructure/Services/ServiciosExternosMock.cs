using API_Banco.Application.Common;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;

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

public class ConfiguracionPagosMock : IConfiguracionDistribucionPagos
{
    public Task<int> ObtenerIdCuentaPrestadoraAsync(TipoServicioPublico tipoServicio, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(2); // Suponemos que la cuenta con ID 2 es la de la Empresa (Luz, UMG)
    }

    public Task<int> ObtenerIdCuentaCorrienteComisionesBancoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1); // Suponemos que la cuenta con ID 1 es la del Banco (El 5% de comisión)
    }
}