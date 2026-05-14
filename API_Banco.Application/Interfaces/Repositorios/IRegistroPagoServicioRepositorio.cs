using API_Banco.Domain.Entities;

namespace API_Banco.Application.Interfaces.Repositorios;

/// <summary>
/// Persistencia de registros de pago de servicios.
/// </summary>
public interface IRegistroPagoServicioRepositorio
{
    Task RegistrarAsync(RegistroPagoServicio registro, CancellationToken cancellationToken = default);
}
