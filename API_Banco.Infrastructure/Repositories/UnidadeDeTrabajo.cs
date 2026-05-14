using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Infrastructure.Persistence;

namespace API_Banco.Infrastructure.Repositories;

public class UnidadDeTrabajo(BancoDbContext context) : IUnidadDeTrabajo
{
    public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        // Confirma todos los cambios acumulados en el DbContext de una sola vez
        return await context.SaveChangesAsync(cancellationToken);
    }
}