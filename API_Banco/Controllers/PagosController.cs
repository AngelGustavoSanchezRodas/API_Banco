using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    /// <summary>
    /// Pagos de servicios públicos. <c>tipoServicio</c> 1 = Universidad, 2 = Telefonía, 3 = Energía.
    /// </summary>
    /// <remarks>
    /// <para><b>Autenticación por endpoint:</b></para>
    /// <list type="bullet">
    ///   <item>Todos los endpoints son públicos en este proyecto académico para
    ///     simplificar la integración con las APIs externas (Universidad, Energía,
    ///     Telefonía) y sus frontends.</item>
    ///   <item>La policy <c>"PortalOSocioBancario"</c> sigue registrada en
    ///     <c>Program.cs</c> (acepta JWT o <c>X-Api-Key</c>). Para volver a proteger
    ///     <c>POST /api/Pagos/ejecutar</c>, reemplaza <c>[AllowAnonymous]</c> en ese
    ///     método por <c>[Authorize(Policy = "PortalOSocioBancario")]</c>.</item>
    /// </list>
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class PagosController : ControllerBase
    {
        private readonly IPagoServiciosServicio _pagoServiciosServicio;

        public PagosController(IPagoServiciosServicio pagoServiciosServicio)
        {
            _pagoServiciosServicio = pagoServiciosServicio;
        }

        [HttpPost("validar")]
        [AllowAnonymous]
        public async Task<IActionResult> Validar([FromBody] ValidacionIdentificadorDto dto)
        {
            var resultado = await _pagoServiciosServicio.ValidarIdentificadorAsync(dto);
            if (!resultado.Exito)
                return BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });

            if (resultado.Valor is { EsValido: false } invalido)
                return BadRequest(new { mensaje = invalido.Mensaje ?? "Identificador no válido.", error = invalido.Mensaje ?? "Identificador no válido.", detalles = resultado.Detalles });

            return Ok(resultado.Valor);
        }

        [HttpPost("ejecutar")]
        [AllowAnonymous] // Académico: abierto a las APIs de Universidad/Energía/Telefonía sin token.
        public async Task<IActionResult> EjecutarPago([FromBody] PagoServicioDto dto)
        {
            var resultado = await _pagoServiciosServicio.EjecutarPagoServicioAsync(dto);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpGet("consultar-deuda/{tipoServicio:int}/{identificador}")]
        [AllowAnonymous]
        public async Task<IActionResult> ConsultarDeuda(int tipoServicio, string identificador)
        {
            var resultado = await _pagoServiciosServicio.ConsultarDeudaAsync(tipoServicio, identificador);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        /// <summary>
        /// Registra un pago de servicio realizado físicamente en ventanilla del
        /// banco (efectivo). No requiere tarjeta ni PIN: el operador del banco
        /// recibe el dinero del cliente y el sistema lo distribuye 95/5.
        /// </summary>
        /// <remarks>
        /// Restringido a <c>ADMIN</c> porque representa una operación de caja
        /// realizada por personal del banco. Mantiene las mismas comisiones,
        /// validaciones de identificador y notificación a la prestadora que el
        /// flujo del cuentahabiente.
        /// </remarks>
        [HttpPost("ventanilla")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> EjecutarPagoVentanilla(
            [FromBody] PagoVentanillaDto dto,
            CancellationToken cancellationToken)
        {
            var resultado = await _pagoServiciosServicio.EjecutarPagoVentanillaAsync(dto, cancellationToken);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }
    }
}