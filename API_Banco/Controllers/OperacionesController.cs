using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using API_Banco.Application.DTOs.Operaciones;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OperacionesController : ControllerBase
    {
        private readonly IOperacionesFinancierasServicio _operacionesServicio;
        private readonly ICuentaRepositorio _cuentaRepositorio;

        public OperacionesController(
            IOperacionesFinancierasServicio operacionesServicio,
            ICuentaRepositorio cuentaRepositorio)
        {
            _operacionesServicio = operacionesServicio;
            _cuentaRepositorio = cuentaRepositorio;
        }

        [HttpPost("deposito")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> Depositar([FromBody] DepositoDto dto, CancellationToken cancellationToken)
        {
            var pertenece = await ClienteDelTokenEsDuenoDeAsync(dto.IdCuenta, cancellationToken);
            if (!pertenece) return Forbid();

            var resultado = await _operacionesServicio.DepositarAsync(dto, cancellationToken);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpPost("retiro")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> Retirar([FromBody] RetiroDto dto, CancellationToken cancellationToken)
        {
            var pertenece = await ClienteDelTokenEsDuenoDeAsync(dto.IdCuenta, cancellationToken);
            if (!pertenece) return Forbid();

            var resultado = await _operacionesServicio.RetirarAsync(dto, cancellationToken);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        [HttpGet("saldo/{idCuenta}")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> ConsultarSaldo(int idCuenta, CancellationToken cancellationToken)
        {
            var pertenece = await ClienteDelTokenEsDuenoDeAsync(idCuenta, cancellationToken);
            if (!pertenece) return Forbid();

            var resultado = await _operacionesServicio.ConsultarSaldoDisponibleAsync(idCuenta, cancellationToken);
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
        public async Task<IActionResult> Transferir([FromBody] TransferirRequestDto dto, CancellationToken cancellationToken)
        {
            // Solo se valida la cuenta ORIGEN: el destino es legítimamente de otro cliente.
            var pertenece = await ClienteDelTokenEsDuenoDeAsync(dto.IdCuentaOrigen, cancellationToken);
            if (!pertenece) return Forbid();

            var resultado = await _operacionesServicio.TransferirAsync(
                dto.IdCuentaOrigen,
                dto.IdCuentaDestino,
                dto.Monto,
                dto.Descripcion);
            return resultado.Exito
                ? Ok(resultado.Valor)
                : BadRequest(new { mensaje = resultado.MensajeError, error = resultado.MensajeError, detalles = resultado.Detalles });
        }

        /// <summary>
        /// Devuelve true si la cuenta indicada pertenece al cliente identificado por
        /// el claim <c>idCliente</c> del JWT. Los ADMIN pasan siempre por este filtro
        /// (no se aplica a sus endpoints), por lo que aquí basta con asegurar
        /// pertenencia para el rol CLIENTE.
        /// </summary>
        private async Task<bool> ClienteDelTokenEsDuenoDeAsync(int idCuenta, CancellationToken cancellationToken)
        {
            if (idCuenta <= 0) return false;

            var idClienteToken = User.FindFirst("idCliente")?.Value ?? User.FindFirst("IdCliente")?.Value;
            if (string.IsNullOrEmpty(idClienteToken) || !int.TryParse(idClienteToken, out var idCliente))
                return false;

            return await _cuentaRepositorio.PerteneceAClienteAsync(idCuenta, idCliente, cancellationToken);
        }
    }

    // 1. DTO para Activar Cuenta (Sin la palabra 'property:')
    public record ActivarCuentaRequestDto(
        int IdCuenta,
        [Range(typeof(decimal), "100.00", "79228162514264337593543950335", ErrorMessage = "El depósito de activación inicial debe ser de al menos Q100.00.")]
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
