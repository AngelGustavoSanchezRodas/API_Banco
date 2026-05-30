using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_Banco.Application.Interfaces;
using API_Banco.Infrastructure.Persistence;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API_Banco.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly BancoDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHasherCredenciales _hasher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            BancoDbContext context,
            IConfiguration config,
            IHasherCredenciales hasher,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _hasher = hasher;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Credencial) || string.IsNullOrWhiteSpace(request.Password))
                return Unauthorized();

            // Tracking habilitado: si el password está en texto plano lo re-hasheamos
            // en este mismo request para migrar transparentemente al esquema BCrypt.
            var usuario = await _context.UsuariosAcceso
                .FirstOrDefaultAsync(u => u.CorreoElectronico == request.Credencial || u.NombreUsuario == request.Credencial, cancellationToken);

            if (usuario is null || !_hasher.Verificar(request.Password, usuario.PasswordHash))
                return Unauthorized();

            if (!_hasher.EsHashValido(usuario.PasswordHash))
            {
                usuario.PasswordHash = _hasher.Hashear(request.Password);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo migrar la password de '{Credencial}' a hash BCrypt. El login fue aceptado igualmente.",
                        request.Credencial);
                }
            }

            // 1. Crear los "Claims" (Los datos cifrados dentro del token)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
                new Claim("idCliente", usuario.IdCliente.ToString()),
                new Claim(ClaimTypes.Role, usuario.Rol) // ¡VITAL para que [Authorize(Roles="...")] funcione!
            };

            // 2. Traer la llave secreta del appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Crear el token JWT
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // Expira en 2 horas
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // 4. Retornar los datos al Front-End EXACTAMENTE como los espera el LoginView.jsx
            var respuesta = new LoginResponse(
                usuario.IdUsuario,
                usuario.IdCliente,
                usuario.Rol,
                tokenString); // ¡Ahora sí enviamos un JWT real!

            return Ok(respuesta);
        }
    }

    public sealed record LoginRequest(string Credencial, string Password);

    // Nota: 'Rol' y 'IdCliente' deben coincidir con la desestructuración en el React
    public sealed record LoginResponse(int IdUsuario, int IdCliente, string Rol, string Token);
}
