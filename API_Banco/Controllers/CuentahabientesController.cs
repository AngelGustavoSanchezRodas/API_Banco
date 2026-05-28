using Microsoft.AspNetCore.Mvc;
using API_Banco.Application.DTOs.Cuentahabientes;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Interfaces.Servicios;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentahabientesController : ControllerBase
    {
        private readonly ICuentahabienteServicio _cuentahabienteServicio;
        private readonly ICuentaRepositorio _cuentaRepositorio;

        public CuentahabientesController(
            ICuentahabienteServicio cuentahabienteServicio,
            ICuentaRepositorio cuentaRepositorio)
        {
            _cuentahabienteServicio = cuentahabienteServicio;
            _cuentaRepositorio = cuentaRepositorio;
        }

        [HttpGet("{idCliente:int}/cuentas")]
        public async Task<IActionResult> ListarCuentas(int idCliente, CancellationToken cancellationToken)
        {
            if (idCliente <= 0)
                return BadRequest("IdCliente no válido.");

            var cuentas = await _cuentaRepositorio.ListarPorClienteAsync(idCliente, cancellationToken);
            return Ok(cuentas);
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
