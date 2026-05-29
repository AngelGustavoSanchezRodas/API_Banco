using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using API_Banco.Application.DTOs.Cuentahabientes;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;
using System.Security.Claims; // [NUEVO] Necesario para leer los claims del token

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // [CAMBIO CRÍTICO] Exigimos que el usuario esté logueado, pero validaremos el rol en cada método
    public class CuentahabientesController : ControllerBase
    {
        private readonly ICuentahabienteServicio _cuentahabienteServicio;
        private readonly ICuentaRepositorio _cuentaRepositorio;

        public CuentahabientesController(
            ICuentahabienteServicio cuentahabienteServicio,
            ICuentaRepositorio cuentaRepositorio)
        {
            _cuentahabienteServicio = cuentahabienteServicio;
            _cuentaRepositorio = cuentaRepositorio;
        }

        [HttpGet("{idCliente:int}/cuentas")]
        [Authorize(Roles = "ADMIN,CLIENTE")] // [SEGURIDAD] Permitimos a ambos roles entrar al flujo
        public async Task<IActionResult> ListarCuentas(int idCliente, CancellationToken cancellationToken)
        {
            if (idCliente <= 0)
                return BadRequest("IdCliente no válido.");

            
            // Leemos el rol. Soportamos tanto el mapeo de .NET como el claim directo "role"
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            if (string.Equals(rol, "CLIENTE", StringComparison.OrdinalIgnoreCase))
            {
                // Extraemos el IdCliente encriptado en el token. Buscamos en minúscula y mayúscula por precaución.
                var idClienteToken = User.FindFirst("idCliente")?.Value ?? User.FindFirst("IdCliente")?.Value;

                // Si el token no tiene IdCliente o el cliente intenta ver cuentas ajenas -> Bloqueo Inmediato (403)
                if (string.IsNullOrEmpty(idClienteToken) || idClienteToken != idCliente.ToString())
                {
                    return Forbid();
                }
            }
            // ========================================================================

            var cuentas = await _cuentaRepositorio.ListarPorClienteAsync(idCliente, cancellationToken);
            return Ok(cuentas);
        }

        [HttpPost("perfil")]
        [Authorize(Roles = "ADMIN")] // [SEGURIDAD] Operación de escritura exclusiva para administradores
        public async Task<IActionResult> CrearPerfil([FromBody] CrearCuentahabienteDto dto)
        {
            var resultado = await _cuentahabienteServicio.CrearPerfilAsync(dto);

            if (!resultado.Exito)
                return BadRequest(new { error = resultado.MensajeError, detalles = resultado.Detalles });

            return Ok(resultado.Valor);
        }

        [HttpPost("tarjeta")]
        [Authorize(Roles = "ADMIN")] // [SEGURIDAD] Operación de escritura exclusiva para administradores
        public async Task<IActionResult> AsociarTarjeta([FromBody] AsociarTarjetaDebitoDto dto)
        {
            var resultado = await _cuentahabienteServicio.AsociarTarjetaDebitoAsync(dto);

            if (!resultado.Exito)
                return BadRequest(new { error = resultado.MensajeError, detalles = resultado.Detalles });

            return Ok(resultado.Valor);
        }
    }
}