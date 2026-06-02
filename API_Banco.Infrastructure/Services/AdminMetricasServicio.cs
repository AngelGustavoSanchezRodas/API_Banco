using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Admin;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Infrastructure.Services;

/// <summary>
/// Implementación read-only del cálculo de métricas administrativas.
///
/// Toda la agregación ocurre del lado del motor de base de datos:
/// no se materializan filas individuales en memoria, lo que mantiene
/// el dashboard rápido incluso con padrones grandes.
/// </summary>
public sealed class AdminMetricasServicio : IAdminMetricasServicio
{
    private readonly BancoDbContext _context;
    private readonly IEstadoRepositorio _estados;
    private readonly ITipoTransaccionRepositorio _tiposTransaccion;
    private readonly IProveedorFecha _fecha;

    public AdminMetricasServicio(
        BancoDbContext context,
        IEstadoRepositorio estados,
        ITipoTransaccionRepositorio tiposTransaccion,
        IProveedorFecha fecha)
    {
        _context = context;
        _estados = estados;
        _tiposTransaccion = tiposTransaccion;
        _fecha = fecha;
    }

    public async Task<MetricasAdminDto> ObtenerMetricasAsync(CancellationToken cancellationToken = default)
    {
        // El "hoy" y "este mes" se calculan desde la perspectiva del banco
        // en hora local Guatemala (UTC-6 sin DST). Después convertimos los
        // bordes a UTC para que el query encaje con los timestamps almacenados.
        var ahoraUtc = _fecha.ObtenerUtcAhora();
        var ahoraLocal = _fecha.DesdeUtcALocal(ahoraUtc);

        var hoyDesdeLocal = new DateTime(
            ahoraLocal.Year, ahoraLocal.Month, ahoraLocal.Day, 0, 0, 0,
            DateTimeKind.Unspecified);
        var hoyHastaLocal = hoyDesdeLocal.AddDays(1);

        var mesDesdeLocal = new DateTime(
            ahoraLocal.Year, ahoraLocal.Month, 1, 0, 0, 0,
            DateTimeKind.Unspecified);
        var mesHastaLocal = mesDesdeLocal.AddMonths(1);

        var hoyDesde = _fecha.DesdeLocalAUtc(hoyDesdeLocal);
        var hoyHasta = _fecha.DesdeLocalAUtc(hoyHastaLocal);
        var mesDesde = _fecha.DesdeLocalAUtc(mesDesdeLocal);
        var mesHasta = _fecha.DesdeLocalAUtc(mesHastaLocal);

        // Resolver el IdEstado "INACTIVO" en BD (puede variar por seed).
        var idEstadoInactivo = await _estados
            .ObtenerIdPorCodigoAsync(CodigosEstado.Inactivo, cancellationToken)
            .ConfigureAwait(false);

        // Tipos de transacción "primarios": los que cuentan como una operación
        // real desde el punto de vista del cliente (no las contrapartidas que
        // genera el banco internamente para que la bitácora cuadre).
        //
        // Incluimos PagoVentanillaIngresoEfectivo para que los pagos realizados
        // en caja del banco también se reflejen en "operaciones hoy" y
        // "volumen mensual", ya que son cobros reales que el banco generó.
        var codigosPrimarios = new[]
        {
            CodigosTipoTransaccion.Deposito,
            CodigosTipoTransaccion.Retiro,
            CodigosTipoTransaccion.TransferenciaOrigen,
            CodigosTipoTransaccion.PagoServicioDebitoCuentahabiente,
            CodigosTipoTransaccion.PagoVentanillaIngresoEfectivo,
        };

        var idsTiposPrimarios = new List<int>(codigosPrimarios.Length);
        foreach (var codigo in codigosPrimarios)
        {
            var id = await _tiposTransaccion
                .ObtenerIdPorCodigoDescripcionAsync(codigo, cancellationToken)
                .ConfigureAwait(false);
            if (id.HasValue) idsTiposPrimarios.Add(id.Value);
        }

        // Las 4 queries se ejecutan en paralelo contra la misma DbContext
        // de manera secuencial (EF no permite múltiples queries concurrentes
        // sobre el mismo contexto). Se mantienen aquí en orden por claridad.

        var clientesRegistrados = await _context.Clientes
            .AsNoTracking()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var cuentasInactivas = idEstadoInactivo.HasValue
            ? await _context.Cuentas
                .AsNoTracking()
                .CountAsync(c => c.IdEstado == idEstadoInactivo.Value, cancellationToken)
                .ConfigureAwait(false)
            : 0;

        var operacionesHoy = idsTiposPrimarios.Count > 0
            ? await _context.TransaccionesBanco
                .AsNoTracking()
                .Where(t => t.Fecha >= hoyDesde && t.Fecha < hoyHasta)
                .Where(t => idsTiposPrimarios.Contains(t.IdTipoTransaccion))
                .CountAsync(cancellationToken)
                .ConfigureAwait(false)
            : 0;

        var volumenMensual = idsTiposPrimarios.Count > 0
            ? await _context.TransaccionesBanco
                .AsNoTracking()
                .Where(t => t.Fecha >= mesDesde && t.Fecha < mesHasta)
                .Where(t => idsTiposPrimarios.Contains(t.IdTipoTransaccion))
                .SumAsync(t => (decimal?)t.Monto, cancellationToken)
                .ConfigureAwait(false) ?? 0m
            : 0m;

        return new MetricasAdminDto(
            ClientesRegistrados: clientesRegistrados,
            OperacionesHoy: operacionesHoy,
            VolumenMensual: volumenMensual,
            CuentasInactivas: cuentasInactivas,
            GeneradoUtc: ahoraUtc);
    }
}
