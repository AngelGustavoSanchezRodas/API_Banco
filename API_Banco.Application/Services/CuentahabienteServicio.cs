using API_Banco.Application.Common;
using API_Banco.Application.Constants;
using API_Banco.Application.DTOs.Cuentahabientes;
using API_Banco.Application.Interfaces;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using API_Banco.Application.Services.Internos;

namespace API_Banco.Application.Services;

/// <summary>
/// Orquesta la creación de perfiles, apertura de cuentas con saldo inicial y asociación de tarjetas de débito.
/// </summary>
public sealed class CuentahabienteServicio(
    IClienteRepositorio clientes,
    ICuentaRepositorio cuentas,
    ITarjetaDebitoRepositorio tarjetas,
    IEstadoRepositorio estados,
    INumeroCuentaGenerador numerosCuenta,
    INumeroTarjetaGenerador numerosTarjeta,
    IUnidadDeTrabajo unidadDeTrabajo) : ICuentahabienteServicio
{
    /// <inheritdoc />
    public async Task<ResultadoOperacion<CuentahabienteCreadoDto>> CrearPerfilAsync(
        CrearCuentahabienteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ValidadoresEntrada.EsDpiPlausible(dto.Dpi))
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("El DPI no es válido.");

        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("Nombre y apellido son obligatorios.");

        if (await clientes.ExisteDpiAsync(dto.Dpi.Trim(), cancellationToken).ConfigureAwait(false))
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("Ya existe un cuentahabiente con el mismo DPI.");

        await clientes.RegistrarPendienteAsync(
                dto.Dpi.Trim(),
                dto.Nombre.Trim(),
                dto.Apellido.Trim(),
                string.IsNullOrWhiteSpace(dto.Celular) ? null : dto.Celular.Trim(),
                string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                cancellationToken)
            .ConfigureAwait(false);

        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var insertado = await clientes.ObtenerPorDpiAsync(dto.Dpi.Trim(), cancellationToken).ConfigureAwait(false);
        if (insertado is null)
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("No se pudo recuperar el cuentahabiente recién creado.");

        var nombreCompleto = $"{insertado.Nombre} {insertado.Apellido}".Trim();
        var resultado = new CuentahabienteCreadoDto(insertado.IdCliente, insertado.Dpi, nombreCompleto);
        return ResultadoOperacion<CuentahabienteCreadoDto>.Ok(resultado);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<CuentaAbiertaDto>> AbrirCuentaConSaldoInicialAsync(
        AbrirCuentaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.IdCliente <= 0)
            return ResultadoOperacion<CuentaAbiertaDto>.Fallo("El identificador de cliente no es válido.");

        if (dto.SaldoInicial < 0)
            return ResultadoOperacion<CuentaAbiertaDto>.Fallo("El saldo inicial no puede ser negativo.");

        var cliente = await clientes.ObtenerEntidadPorIdAsync(dto.IdCliente, cancellationToken).ConfigureAwait(false);
        if (cliente is null)
            return ResultadoOperacion<CuentaAbiertaDto>.Fallo("No se encontró el cuentahabiente.");

        var idEstadoActivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        if (idEstadoActivo is null)
            return ResultadoOperacion<CuentaAbiertaDto>.Fallo("No está configurado el estado ACTIVO para cuentas.");

        var noCuenta = await numerosCuenta.GenerarSiguienteNumeroCuentaAsync(cancellationToken).ConfigureAwait(false);
        await cuentas
            .RegistrarCuentaPendienteAsync(noCuenta, dto.SaldoInicial, cliente, idEstadoActivo.Value, cancellationToken)
            .ConfigureAwait(false);

        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var cuenta = await cuentas.ObtenerPorNumeroAsync(noCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<CuentaAbiertaDto>.Fallo("No se pudo recuperar la cuenta recién creada.");

        var respuesta = new CuentaAbiertaDto(cuenta.IdCuenta, cuenta.NoCuenta, cuenta.Saldo, cuenta.IdCliente);
        return ResultadoOperacion<CuentaAbiertaDto>.Ok(respuesta);
    }

    /// <inheritdoc />
    public async Task<ResultadoOperacion<TarjetaDebitoDto>> AsociarTarjetaDebitoAsync(
        AsociarTarjetaDebitoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.IdCliente <= 0 || dto.IdCuenta <= 0)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("Cliente o cuenta no válidos.");

        if (string.IsNullOrWhiteSpace(dto.Pin))
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("El PIN es obligatorio.");

        if (dto.FechaVencimiento.Date <= DateTime.UtcNow.Date)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("La fecha de vencimiento debe ser futura.");

        var pertenece = await cuentas.PerteneceAClienteAsync(dto.IdCuenta, dto.IdCliente, cancellationToken).ConfigureAwait(false);
        if (!pertenece)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("La cuenta no pertenece al cuentahabiente indicado.");

        var idEstadoTarjeta = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        if (idEstadoTarjeta is null)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("No está configurado el estado ACTIVO para tarjetas.");

        var numeroTarjeta = await numerosTarjeta.GenerarSiguienteNumeroTarjetaAsync(cancellationToken).ConfigureAwait(false);
        await tarjetas
            .RegistrarTarjetaPendienteAsync(
                dto.IdCuenta,
                numeroTarjeta,
                dto.Pin.Trim(),
                dto.FechaVencimiento,
                idEstadoTarjeta.Value,
                cancellationToken)
            .ConfigureAwait(false);

        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var tarjeta = await tarjetas.ObtenerUltimaPorCuentaAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (tarjeta is null)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("No se pudo recuperar la tarjeta recién asociada.");

        var salida = new TarjetaDebitoDto(
            tarjeta.IdTarjeta,
            FormateoTarjeta.Enmascarar(tarjeta.NumeroTarjeta),
            tarjeta.IdCuenta,
            tarjeta.FechaVencimiento);

        return ResultadoOperacion<TarjetaDebitoDto>.Ok(salida);
    }
}
