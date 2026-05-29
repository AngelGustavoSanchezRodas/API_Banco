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
    [AllowAnonymous]
    public class PagosController : ControllerBase
    {
        private readonly IPagoServiciosServicio _pagoServiciosServicio;

        public PagosController(IPagoServiciosServicio pagoServiciosServicio)
        {
            _pagoServiciosServicio = pagoServiciosServicio;
        }

        [HttpPost("validar")]
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
        public async Task<IActionResult> EjecutarPago([FromBody] PagoServicioDto dto)
        {
            // Ejecución directa sin validación de identidad. 
            // El Frontend y las APIs externas usan este mismo endpoint libremente.
            var resultado = await _pagoServiciosServicio.EjecutarPagoServicioAsync(dto);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpGet("consultar-deuda/{tipoServicio:int}/{identificador}")]
        public async Task<IActionResult> ConsultarDeuda(int tipoServicio, string identificador)
        {
            var resultado = await _pagoServiciosServicio.ConsultarDeudaAsync(tipoServicio, identificador);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }
    }
}