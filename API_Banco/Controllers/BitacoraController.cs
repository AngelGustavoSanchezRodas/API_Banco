using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Banco.Application.DTOs.Bitacora;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BitacoraController(
        IBitacoraServicio bitacoraServicio,
        ICuentaRepositorio cuentaRepositorio) : ControllerBase
    {
        [HttpGet("kardex/{idCuenta}")]
        [Authorize(Roles = "ADMIN,CLIENTE")]
        public async Task<IActionResult> ObtenerKardex(
            int idCuenta,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            CancellationToken cancellationToken)
        {
            // Si el solicitante es CLIENTE, debe ser dueño de la cuenta consultada.
            // El ADMIN puede consultar el kardex de cualquier cuenta (revisión interna).
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            if (string.Equals(rol, "CLIENTE", StringComparison.OrdinalIgnoreCase))
            {
                var idClienteToken = User.FindFirst("idCliente")?.Value ?? User.FindFirst("IdCliente")?.Value;
                if (string.IsNullOrEmpty(idClienteToken) || !int.TryParse(idClienteToken, out var idCliente))
                    return Forbid();

                var pertenece = await cuentaRepositorio.PerteneceAClienteAsync(idCuenta, idCliente, cancellationToken);
                if (!pertenece) return Forbid();
            }

            var filtro = new FiltroBitacoraDto(idCuenta, desde, hasta);
            var resultado = await bitacoraServicio.ObtenerKardexAsync(filtro, cancellationToken);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }
    }
}