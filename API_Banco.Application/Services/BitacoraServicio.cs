using API_Banco.Application.Common;
using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Bitacora;
using API_Banco.Application.DTOs.Notificaciones;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services.Internos;

namespace API_Banco.Application.Services;

/// <summary>
/// Consulta de kardex / bitácora cronológica por cuenta.
/// </summary>
public sealed class BitacoraServicio(ITransaccionRepositorio transacciones) : IBitacoraServicio
{
    /// <inheritdoc />
    public async Task<ResultadoOperacion<IReadOnlyList<MovimientoBitacoraDto>>> ObtenerKardexAsync(
        FiltroBitacoraDto filtro,
        CancellationToken cancellationToken = default)
    {
        if (filtro.IdCuenta <= 0)
            return ResultadoOperacion<IReadOnlyList<MovimientoBitacoraDto>>.Fallo("El identificador de cuenta no es válido.");

        var items = await transacciones
            .ListarPorCuentaOrdenCronologicoAsync(filtro.IdCuenta, filtro.DesdeUtc, filtro.HastaUtc, cancellationToken)
            .ConfigureAwait(false);

        var salida = items
            .Select(x => new MovimientoBitacoraDto(
                x.IdTransaccion,
                x.IdCuenta,
                x.Monto,
                x.CodigoTipoTransaccion,
                x.DescripcionTipoTransaccion,
                x.FechaUtc))
            .ToList();

        return ResultadoOperacion<IReadOnlyList<MovimientoBitacoraDto>>.Ok(salida);
    }
}
