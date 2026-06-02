namespace API_Banco.Application.Interfaces;

/// <summary>
/// Abstrae la fecha/hora del sistema. Expone tanto UTC (para timestamps
/// almacenados en BD y comparaciones internas) como la hora local del banco
/// (Guatemala, UTC-6 sin horario de verano) para reportes y agrupaciones
/// diarias/mensuales orientadas al usuario.
/// </summary>
public interface IProveedorFecha
{
    /// <summary>
    /// Zona horaria operativa del banco. Constante: <c>America/Guatemala</c>
    /// (UTC-6, sin DST).
    /// </summary>
    TimeZoneInfo ZonaHoraria { get; }

    /// <summary>Marca de tiempo actual en UTC.</summary>
    DateTime ObtenerUtcAhora();

    /// <summary>
    /// Marca de tiempo actual en hora local del banco (Guatemala).
    /// Útil para construir rangos "hoy", "este mes", etc. desde la perspectiva
    /// del operador.
    /// </summary>
    DateTime ObtenerLocalAhora();

    /// <summary>
    /// Convierte un <see cref="DateTime"/> en hora local del banco (Guatemala) a UTC.
    /// Si el valor recibido tiene <c>Kind=Utc</c> se devuelve sin transformación.
    /// </summary>
    DateTime DesdeLocalAUtc(DateTime localGuatemala);

    /// <summary>
    /// Convierte un <see cref="DateTime"/> UTC a la hora local del banco (Guatemala).
    /// </summary>
    DateTime DesdeUtcALocal(DateTime utc);
}
