using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    /// <summary>
    /// Pagos de servicios públicos. <c>tipoServicio</c> 1 = Universidad, 2 = Telefonía, 3 = Energía.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "CLIENTE")]
    public class PagosController : ControllerBase
    {
        private readonly IPagoServiciosServicio _pagoServiciosServicio;

        public PagosController(IPagoServiciosServicio pagoServiciosServicio)
        {
            _pagoServiciosServicio = pagoServiciosServicio;
        }

        /// <summary>Valida el identificador ante la empresa (carné, teléfono, contador).</summary>
        [HttpPost("validar")]
        public async Task<IActionResult> Validar([FromBody] ValidacionIdentificadorDto dto)
        {
            var resultado = await _pagoServiciosServicio.ValidarIdentificadorAsync(dto);
            if (!resultado.Exito)
                return BadRequest(RespuestaError(resultado.MensajeError));

            if (resultado.Valor is { EsValido: false } invalido)
                return BadRequest(RespuestaError(invalido.Mensaje ?? "Identificador no válido."));

            return Ok(resultado.Valor);
        }

        /// <summary>Ejecuta cobro con tarjeta/PIN y distribución 95/5.</summary>
        [HttpPost("ejecutar")]
        public async Task<IActionResult> EjecutarPago([FromBody] PagoServicioDto dto)
        {
            var resultado = await _pagoServiciosServicio.EjecutarPagoServicioAsync(dto);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(RespuestaError(resultado.MensajeError, resultado.Detalles));
        }

        /// <summary>Consulta deuda pendiente (postpago). Prepago telefonía devuelve 0.</summary>
        [HttpGet("consultar-deuda/{tipoServicio:int}/{identificador}")]
        public async Task<IActionResult> ConsultarDeuda(int tipoServicio, string identificador)
        {
            var resultado = await _pagoServiciosServicio.ConsultarDeudaAsync(tipoServicio, identificador);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(RespuestaError(resultado.MensajeError));
        }

        private static object RespuestaError(string? mensaje, IReadOnlyList<string>? detalles = null)
        {
            var texto = mensaje ?? "Operación rechazada.";
            return new
            {
                mensaje = texto,
                message = texto,
                error = texto,
                detail = texto,
                detalles
            };
        }
    }
}
