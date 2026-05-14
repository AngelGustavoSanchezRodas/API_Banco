using Microsoft.AspNetCore.Mvc;
using API_Banco.Application.DTOs.Bitacora;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitacoraController(IBitacoraServicio bitacoraServicio) : ControllerBase
    {
        [HttpGet("kardex/{idCuenta}")]
        public async Task<IActionResult> ObtenerKardex(int idCuenta, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var filtro = new FiltroBitacoraDto(idCuenta, desde, hasta);
            var resultado = await bitacoraServicio.ObtenerKardexAsync(filtro);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }
    }
}