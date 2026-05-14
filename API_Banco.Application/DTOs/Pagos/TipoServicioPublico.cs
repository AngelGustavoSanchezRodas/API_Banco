namespace API_Banco.Application.DTOs.Pagos;

/// <summary>
/// Empresa receptora del pago y tipo de identificador único exigido en la orquestación.
/// </summary>
public enum TipoServicioPublico
{
    /// <summary>Pago universitario validado por código de carné.</summary>
    Universidad = 1,

    /// <summary>Pago de telefonía validado por número telefónico.</summary>
    Telefonia = 2,

    /// <summary>Pago de energía eléctrica validado por número de contador.</summary>
    EnergiaElectrica = 3
}
