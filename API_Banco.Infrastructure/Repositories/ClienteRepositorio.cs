using Microsoft.EntityFrameworkCore;
using API_Banco.Application.Constants;
using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Application.Persistencia;
using API_Banco.Domain.Entities;
using API_Banco.Infrastructure.Persistence;

namespace API_Banco.Infrastructure.Repositories
{
    public class ClienteRepositorio : IClienteRepositorio
    {
        private readonly BancoDbContext _context;

        public ClienteRepositorio(BancoDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExisteDpiAsync(string dpi, CancellationToken cancellationToken = default)
        {
            return await _context.Clientes.AnyAsync(c => c.Dpi == dpi, cancellationToken);
        }

        public async Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Clientes.AnyAsync(c => c.Email == email, cancellationToken);
        }

        public async Task<bool> ExisteEmailEnOtroClienteAsync(
            string email,
            int idClienteActual,
            CancellationToken cancellationToken = default)
        {
            return await _context.Clientes
                .AsNoTracking()
                .AnyAsync(
                    c => c.Email == email && c.IdCliente != idClienteActual,
                    cancellationToken);
        }

        public async Task<CuentahabienteResumen?> ObtenerPorIdAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            // Excluimos al cliente sistema: no es un cuentahabiente real, no debe asomar
            // en consultas del padrón ni habilitar operaciones de cliente sobre él.
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.IdCliente == idCliente && c.Dpi != CodigosClienteSistema.Dpi)
                .Select(c => new CuentahabienteResumen(
                    c.IdCliente, c.Dpi, c.Nombre, c.Apellido, c.Nit, c.Celular, c.Email))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Cliente?> ObtenerEntidadPorIdAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            // No filtramos el cliente sistema aquí: este método se usa internamente
            // (p.ej. al registrar pagos de servicios, donde sí necesitamos las cuentas internas).
            return await _context.Clientes.FirstOrDefaultAsync(c => c.IdCliente == idCliente, cancellationToken);
        }

        public async Task<CuentahabienteResumen?> ObtenerPorDpiAsync(string dpi, CancellationToken cancellationToken = default)
        {
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Dpi == dpi && c.Dpi != CodigosClienteSistema.Dpi)
                .Select(c => new CuentahabienteResumen(
                    c.IdCliente, c.Dpi, c.Nombre, c.Apellido, c.Nit, c.Celular, c.Email))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<CuentahabienteResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        {
            // Sin ORDER BY explícito, MySQL no garantiza un orden estable entre
            // ejecuciones (depende del plan, paginación interna y caches). Para
            // que el padrón en la consola admin se vea siempre en el mismo orden
            // (y los clientes más recientes queden al final), ordenamos por
            // id_cliente ascendente, que coincide con el orden de creación.
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Dpi != CodigosClienteSistema.Dpi)
                .OrderBy(c => c.IdCliente)
                .Select(c => new CuentahabienteResumen(
                    c.IdCliente, c.Dpi, c.Nombre, c.Apellido, c.Nit, c.Celular, c.Email))
                .ToListAsync(cancellationToken);
        }

        public async Task<Cliente> RegistrarPendienteAsync(string dpi, string nit, string nombre, string apellido, string? celular, string? email, CancellationToken cancellationToken = default)
        {
            var nuevoCliente = new Cliente
            {
                Dpi = dpi,
                Nit = nit,
                Nombre = nombre,
                Apellido = apellido,
                Celular = celular,
                Email = email
            };

            await _context.Clientes.AddAsync(nuevoCliente, cancellationToken);
            return nuevoCliente;
        }

        public async Task RegistrarAccesoPendienteAsync(Cliente cliente, string nombreUsuario, string correoElectronico, string passwordHash, string rol, CancellationToken cancellationToken = default)
        {
            var acceso = new UsuarioAcceso
            {
                Cliente = cliente,
                NombreUsuario = nombreUsuario,
                CorreoElectronico = correoElectronico,
                PasswordHash = passwordHash,
                Rol = rol
            };

            await _context.UsuariosAcceso.AddAsync(acceso, cancellationToken);
        }

        public async Task<UsuarioAcceso?> ObtenerAccesoPorIdClienteAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            return await _context.UsuariosAcceso
                .FirstOrDefaultAsync(u => u.IdCliente == idCliente, cancellationToken);
        }
    }
}
