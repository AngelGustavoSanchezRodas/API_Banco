using Microsoft.AspNetCore.Mvc;
using API_Banco.Application.DTOs.Operaciones;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperacionesController(IOperacionesFinancierasServicio operacionesServicio) : ControllerBase
    {
        [HttpPost("deposito")]
        public async Task<IActionResult> Depositar([FromBody] DepositoDto dto)
        {
            var resultado = await operacionesServicio.DepositarAsync(dto);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }

        [HttpPost("retiro")]
        public async Task<IActionResult> Retirar([FromBody] RetiroDto dto)
        {
            var resultado = await operacionesServicio.RetirarAsync(dto);
            return resultado.Exito ? Ok(resultado.Valor) : BadRequest(resultado.MensajeError);
        }

        [HttpGet("saldo/{idCuenta}")]
        public async Task<IActionResult> ConsultarSaldo(int idCuenta)
        {
            var resultado = await operacionesServicio.ConsultarSaldoDisponibleAsync(idCuenta);
            return resultado.Exito ? Ok(resultado.Valor) : NotFound(resultado.MensajeError);
        }
    }
}