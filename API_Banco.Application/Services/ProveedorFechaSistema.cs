using API_Banco.Application.Interfaces;

namespace API_Banco.Application.Services;

/// <summary>
/// Implementación por defecto de <see cref="IProveedorFecha"/>.
///
/// <para>
/// La hora de referencia operativa es <b>America/Guatemala</b> (UTC-6, sin
/// horario de verano). Toda la persistencia sigue trabajando en UTC; este
/// servicio solo expone el "hoy" / "este mes" en hora local para reportes
/// y agrupaciones que el usuario reconoce intuitivamente.
/// </para>
/// </summary>
public sealed class ProveedorFechaSistema : IProveedorFecha
{
    public TimeZoneInfo ZonaHoraria { get; } = ResolverZonaGuatemala();

    public DateTime ObtenerUtcAhora() => DateTime.UtcNow;

    public DateTime ObtenerLocalAhora() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHoraria);

    public DateTime DesdeLocalAUtc(DateTime localGuatemala)
    {
        if (localGuatemala.Kind == DateTimeKind.Utc)
            return localGuatemala;

        // Forzamos Kind=Unspecified para que ConvertTimeToUtc lea la zona desde el parámetro.
        var sinKind = DateTime.SpecifyKind(localGuatemala, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(sinKind, ZonaHoraria);
    }

    public DateTime DesdeUtcALocal(DateTime utc)
    {
        var utcEspecificado = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcEspecificado, ZonaHoraria);
    }

    /// <summary>
    /// Resuelve la zona horaria de Guatemala probando varios identificadores
    /// para ser portable entre Windows, Linux y Mac. Si todo falla, devuelve
    /// una zona personalizada fija de -06:00 (Guatemala no observa DST).
    /// </summary>
    private static TimeZoneInfo ResolverZonaGuatemala()
    {
        // En .NET 6+ con ICU tanto los IDs IANA como los Windows funcionan,
        // pero el orden de soporte cambia según la plataforma del host.
        string[] candidatos =
        {
            "America/Guatemala",            // IANA (Linux, macOS, Windows con tzdata)
            "Central America Standard Time" // Windows clásico
        };

        foreach (var id in candidatos)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { /* siguiente */ }
            catch (InvalidTimeZoneException) { /* siguiente */ }
        }

        // Fallback: zona personalizada UTC-6, sin reglas de DST.
        return TimeZoneInfo.CreateCustomTimeZone(
            id: "America/Guatemala (Fallback)",
            baseUtcOffset: TimeSpan.FromHours(-6),
            displayName: "(UTC-06:00) Guatemala (fallback)",
            standardDisplayName: "CST");
    }
}
