using Microsoft.EntityFrameworkCore;
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

        public async Task<CuentahabienteResumen?> ObtenerPorIdAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCliente == idCliente, cancellationToken);

            if (cliente is null) return null;

            return new CuentahabienteResumen(cliente.IdCliente, cliente.Dpi, cliente.Nombre, cliente.Apellido);
        }

        public async Task<Cliente?> ObtenerEntidadPorIdAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            return await _context.Clientes.FirstOrDefaultAsync(c => c.IdCliente == idCliente, cancellationToken);
        }

        public async Task<CuentahabienteResumen?> ObtenerPorDpiAsync(string dpi, CancellationToken cancellationToken = default)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Dpi == dpi, cancellationToken);

            if (cliente is null) return null;

            return new CuentahabienteResumen(cliente.IdCliente, cliente.Dpi, cliente.Nombre, cliente.Apellido);
        }

        public async Task<Cliente> RegistrarPendienteAsync(string dpi, string nombre, string apellido, string? celular, string? email, CancellationToken cancellationToken = default)
        {
            var nuevoCliente = new Cliente
            {
                Dpi = dpi,
                Nombre = nombre,
                Apellido = apellido,
                Celular = celular,
                Email = email
            };

            await _context.Clientes.AddAsync(nuevoCliente, cancellationToken);
            return nuevoCliente;
        }
    }
}
