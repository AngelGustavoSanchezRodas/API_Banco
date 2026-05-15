using Microsoft.AspNetCore.Mvc;
using API_Banco.Application.DTOs.Cuentahabientes;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentahabientesController : ControllerBase
    {
        private readonly ICuentahabienteServicio _cuentahabienteServicio;

        public CuentahabientesController(ICuentahabienteServicio cuentahabienteServicio)
        {
            _cuentahabienteServicio = cuentahabienteServicio;
        }

        [HttpPost("perfil")]
        public async Task<IActionResult> CrearPerfil([FromBody] CrearCuentahabienteDto dto)
        {
            var resultado = await _cuentahabienteServicio.CrearPerfilAsync(dto);

            if (!resultado.Exito)
                return BadRequest(new { error = resultado.MensajeError, detalles = resultado.Detalles });

            return Ok(resultado.Valor);
        }

        [HttpPost("tarjeta")]
        public async Task<IActionResult> AsociarTarjeta([FromBody] AsociarTarjetaDebitoDto dto)
        {
            var resultado = await _cuentahabienteServicio.AsociarTarjetaDebitoAsync(dto);

            if (!resultado.Exito)
                return BadRequest(new { error = resultado.MensajeError, detalles = resultado.Detalles });

            return Ok(resultado.Valor);
        }
    }
}
