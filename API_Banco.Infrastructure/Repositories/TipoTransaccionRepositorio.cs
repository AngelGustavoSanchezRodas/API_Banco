using API_Banco.Application.Interfaces.Repositorios;
using API_Banco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Infrastructure.Repositories;

public class TipoTransaccionRepositorio(BancoDbContext context) : ITipoTransaccionRepositorio
{
    public async Task<int?> ObtenerIdPorCodigoDescripcionAsync(string codigoDescripcion, CancellationToken cancellationToken = default)
    {
        var tipo = await context.TiposTransaccion
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Descripcion == codigoDescripcion, cancellationToken);

        return tipo?.IdTipoTransaccion;
    }
}