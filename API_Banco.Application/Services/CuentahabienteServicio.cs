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
    IHasherCredenciales hasher,
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

        if (dto.IdTipoCuenta <= 0)
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("El tipo de cuenta no es válido.");

        var dpi = dto.Dpi.Trim();
        var nit = dto.Nit.Trim();
        var nombre = dto.Nombre.Trim();
        var apellido = dto.Apellido.Trim();
        var celular = string.IsNullOrWhiteSpace(dto.Celular) ? null : dto.Celular.Trim();
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        var password = GenerarPasswordTemporal();

        if (string.IsNullOrWhiteSpace(nit))
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("El NIT es obligatorio.");


        if (await clientes.ExisteDpiAsync(dpi, cancellationToken).ConfigureAwait(false))
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("Ya existe un cuentahabiente con el mismo DPI.");

        if (!string.IsNullOrWhiteSpace(email) &&
            await clientes.ExisteEmailAsync(email, cancellationToken).ConfigureAwait(false))
        {
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("Ya existe un cuentahabiente con el mismo email.");
        }

        var cliente = await clientes.RegistrarPendienteAsync(
                dpi,
                nit,
                nombre,
                apellido,
                celular,
                email,
                cancellationToken)
            .ConfigureAwait(false);

        await clientes.RegistrarAccesoPendienteAsync(
                cliente,
                dpi,
                email ?? string.Empty,
                hasher.Hashear(password),
                "CLIENTE",
                cancellationToken)
            .ConfigureAwait(false);

        var idEstadoPendiente = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.PendienteActivacion, cancellationToken).ConfigureAwait(false);
        if (idEstadoPendiente is null)
            return ResultadoOperacion<CuentahabienteCreadoDto>.Fallo("Estado PENDIENTE_ACTIVACION no configurado para cuentas.");

        var noCuenta = await numerosCuenta.GenerarSiguienteNumeroCuentaAsync(cancellationToken).ConfigureAwait(false);
        await cuentas
            .RegistrarCuentaPendienteAsync(
                noCuenta,
                cliente,
                dto.IdTipoCuenta,
                idEstadoPendiente.Value,
                0m,
                cancellationToken)
            .ConfigureAwait(false);

        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}".Trim();
        var resultado = new CuentahabienteCreadoDto(cliente.IdCliente, cliente.Dpi, nombreCompleto, dpi, password);
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
            .RegistrarCuentaPendienteAsync(
                noCuenta,
                cliente,
                1,
                idEstadoActivo.Value,
                dto.SaldoInicial,
                cancellationToken)
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
        if (dto.IdCuenta <= 0)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("La cuenta no es válida.");

        var cuenta = await cuentas.ObtenerEntidadPorIdAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (cuenta is null)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("La cuenta especificada no existe en el core bancario.");

        var idEstadoTarjeta = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Activo, cancellationToken).ConfigureAwait(false);
        if (idEstadoTarjeta is null)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("No está configurado el estado ACTIVO para tarjetas.");

        var idEstadoInactivo = await estados.ObtenerIdPorCodigoAsync(CodigosEstado.Inactivo, cancellationToken).ConfigureAwait(false);
        if (idEstadoInactivo is null)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("No está configurado el estado INACTIVO para tarjetas.");

        // Regla de negocio: una sola tarjeta ACTIVA por cuenta. Si ésta es una
        // reemisión, las tarjetas activas previas pasan a INACTIVO en la misma
        // unidad de trabajo que crea la nueva (todo-o-nada).
        await tarjetas
            .BloquearTarjetasActivasDeCuentaAsync(
                dto.IdCuenta,
                idEstadoTarjeta.Value,
                idEstadoInactivo.Value,
                cancellationToken)
            .ConfigureAwait(false);

        var pin = GenerarPinTemporal();
        var fechaVencimiento = GenerarFechaVencimiento();
        var numeroTarjeta = await numerosTarjeta.GenerarSiguienteNumeroTarjetaAsync(cancellationToken).ConfigureAwait(false);
        await tarjetas
            .RegistrarTarjetaPendienteAsync(
                dto.IdCuenta,
                numeroTarjeta,
                hasher.Hashear(pin),
                fechaVencimiento,
                idEstadoTarjeta.Value,
                cancellationToken)
            .ConfigureAwait(false);

        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken).ConfigureAwait(false);

        var tarjeta = await tarjetas.ObtenerUltimaPorCuentaAsync(dto.IdCuenta, cancellationToken).ConfigureAwait(false);
        if (tarjeta is null)
            return ResultadoOperacion<TarjetaDebitoDto>.Fallo("No se pudo recuperar la tarjeta recién asociada.");

        var salida = new TarjetaDebitoDto(
            tarjeta.NumeroTarjeta,
            tarjeta.FechaVencimiento.Month,
            tarjeta.FechaVencimiento.Year,
            pin);

        return ResultadoOperacion<TarjetaDebitoDto>.Ok(salida);
    }

    private static string GenerarPinTemporal()
    {
        // RandomNumberGenerator es criptográficamente seguro: el rango superior
        // es exclusivo, así que [0, 10_000) cubre exactamente 0000-9999.
        var valor = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 10_000);
        return valor.ToString("D4");
    }


    private static DateTime GenerarFechaVencimiento()
    {
        var ahora = DateTime.UtcNow;
        return new DateTime(ahora.Year + 3, ahora.Month, 1).AddMonths(1).AddDays(-1);
    }

    // Alfabetos sin caracteres ambiguos (sin I/l/1/O/0) para que el admin pueda
    // dictar la password al cliente sin confundir letras y números.
    private const string AlfabetoMayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string AlfabetoMinusculas = "abcdefghijkmnopqrstuvwxyz";
    private const string AlfabetoDigitos = "23456789";
    private const string AlfabetoSimbolos = "@#$%&*+-=?";
    private const int LongitudPasswordTemporal = 12;

    private static string GenerarPasswordTemporal()
    {
        Span<char> buffer = stackalloc char[LongitudPasswordTemporal];

        // Garantizamos al menos un carácter de cada familia, así la password
        // siempre cumple políticas típicas (mayúscula, minúscula, número, símbolo).
        buffer[0] = SacarChar(AlfabetoMayusculas);
        buffer[1] = SacarChar(AlfabetoMinusculas);
        buffer[2] = SacarChar(AlfabetoDigitos);
        buffer[3] = SacarChar(AlfabetoSimbolos);

        var alfabetoCombinado = AlfabetoMayusculas + AlfabetoMinusculas + AlfabetoDigitos + AlfabetoSimbolos;
        for (var i = 4; i < buffer.Length; i++)
            buffer[i] = SacarChar(alfabetoCombinado);

        // Fisher-Yates con RNG criptográfico: mezcla las posiciones para que
        // las garantías mínimas no queden siempre en los primeros 4 caracteres.
        for (var i = buffer.Length - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }

        return new string(buffer);
    }

    private static char SacarChar(string alfabeto)
    {
        var indice = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, alfabeto.Length);
        return alfabeto[indice];
    }
}
