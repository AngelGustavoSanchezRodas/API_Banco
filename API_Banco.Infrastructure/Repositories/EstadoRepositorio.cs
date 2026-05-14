using API_Banco.Application.Interfaces;
using API_Banco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Infrastructure.Repositories;

public class EstadoRepositorio(BancoDbContext context) : IEstadoRepositorio
{
    public async Task<int?> ObtenerIdPorCodigoAsync(string codigoEstado, CancellationToken cancellationToken = default)
    {
        var estado = await context.Estados
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Descripcion == codigoEstado, cancellationToken);
        return estado?.IdEstado;
    }
}