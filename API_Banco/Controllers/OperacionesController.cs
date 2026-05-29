using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using API_Banco.Application.DTOs.Operaciones;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OperacionesController : ControllerBase
    {
        private readonly IOperacionesFinancierasServicio _operacionesServicio;

        public OperacionesController(IOperacionesFinancierasServicio operacionesServicio)
        {
            _operacionesServicio = operacionesServicio;
        }

        [HttpPost("deposito")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> Depositar([FromBody] DepositoDto dto)
        {
            var resultado = await _operacionesServicio.DepositarAsync(dto);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpPost("retiro")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> Retirar([FromBody] RetiroDto dto)
        {
            var resultado = await _operacionesServicio.RetirarAsync(dto);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpGet("saldo/{idCuenta}")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> ConsultarSaldo(int idCuenta)
        {
            var resultado = await _operacionesServicio.ConsultarSaldoDisponibleAsync(idCuenta);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpPost("activar-cuenta")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ActivarCuenta([FromBody] ActivarCuentaRequestDto dto)
        {
            var resultado = await _operacionesServicio.ActivarCuentaConDepositoAsync(dto.IdCuenta, dto.MontoDeposito);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpPost("transferir")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> Transferir([FromBody] TransferirRequestDto dto)
        {
            var resultado = await _operacionesServicio.TransferirAsync(
                dto.IdCuentaOrigen,
                dto.IdCuentaDestino,
                dto.Monto,
                dto.Descripcion);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }
    }

    // 1. DTO para Activar Cuenta (Sin la palabra 'property:')
    public record ActivarCuentaRequestDto(
        int IdCuenta,
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto debe ser mayor a 0.")]
        decimal MontoDeposito
    );

    // 2. DTO para Transferir (Sin la palabra 'property:')
    public record TransferirRequestDto(
        int IdCuentaOrigen,
        int IdCuentaDestino,
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto debe ser mayor a 0.")]
        decimal Monto,
        string Descripcion
    );
}
