namespace API_Banco.Application.Interfaces;

/// <summary>
/// Abstrae la fecha/hora para pruebas y consistencia UTC en la capa de aplicación.
/// </summary>
public interface IProveedorFecha
{
    DateTime ObtenerUtcAhora();
}
