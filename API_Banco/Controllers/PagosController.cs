using Microsoft.AspNetCore.Mvc;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
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
        public async Task<IActionResult> Validar([FromBody] ValidacionIdentificadorDto dto)
        {
            var resultado = await _pagoServiciosServicio.ValidarIdentificadorAsync(dto);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }

        [HttpPost("ejecutar")]
        public async Task<IActionResult> EjecutarPago([FromBody] PagoServicioDto dto)
        {
            var resultado = await _pagoServiciosServicio.EjecutarPagoServicioAsync(dto);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }

        [HttpGet("consultar-deuda/{tipoServicio:int}/{identificador}")]
        public async Task<IActionResult> ConsultarDeuda(int tipoServicio, string identificador)
        {
            var resultado = await _pagoServiciosServicio.ConsultarDeudaAsync(tipoServicio, identificador);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }
    }
}
