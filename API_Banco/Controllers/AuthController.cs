using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_Banco.Infrastructure.Persistence;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(BancoDbContext context) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
                return Unauthorized();

            var usuario = await context.UsuariosAcceso
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.CorreoElectronico == request.Correo, cancellationToken);

            if (usuario is null || usuario.PasswordHash != request.Password)
                return Unauthorized();

            var respuesta = new LoginResponse(
                usuario.IdUsuario,
                usuario.IdCliente,
                usuario.Rol,
                "token-jwt-basico");

            return Ok(respuesta);
        }
    }

    public sealed record LoginRequest(string Correo, string Password);

    public sealed record LoginResponse(int IdUsuario, int IdCliente, string Rol, string Token);
}
