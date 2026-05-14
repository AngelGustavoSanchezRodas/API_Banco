namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Entrada para validar un identificador ante la empresa correspondiente sin ejecutar cobro.
/// </summary>
public sealed record ValidacionIdentificadorDto(TipoServicioPublico TipoServicio, string Identificador);
