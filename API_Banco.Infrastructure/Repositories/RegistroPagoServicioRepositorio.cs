using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Domain.Entities;
using API_Banco.Infrastructure.Persistence;

namespace API_Banco.Infrastructure.Repositories;

public class RegistroPagoServicioRepositorio(BancoDbContext context) : IRegistroPagoServicioRepositorio
{
    public async Task RegistrarAsync(RegistroPagoServicio registro, CancellationToken cancellationToken = default)
    {
        await context.RegistroPagosServicios.AddAsync(registro, cancellationToken);
    }
}
