using Microsoft.AspNetCore.Mvc;
using API_Banco.Application.DTOs.Pagos;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagosController(IPagoServiciosServicio pagosServicio) : ControllerBase
    {
        [HttpPost("validar")]
        public async Task<IActionResult> Validar([FromBody] ValidacionIdentificadorDto dto)
        {
            var resultado = await pagosServicio.ValidarIdentificadorAsync(dto);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }

        [HttpPost("ejecutar")]
        public async Task<IActionResult> EjecutarPago([FromBody] PagoServicioDto dto)
        {
            var resultado = await pagosServicio.EjecutarPagoServicioAsync(dto);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }
    }
}