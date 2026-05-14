namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Unidad de trabajo para confirmar transacciones consistentes entre repositorios (EF Core u otro ORM).
/// </summary>
public interface IUnidadDeTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
