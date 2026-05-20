using API_Banco.Application.DTOs.Pagos;

namespace API_Banco.Application.Constants;

/// <summary>
/// Valores de <c>registro_pagos_servicios.entidad_servicio</c> en base de datos (ENUM).
/// </summary>
public static class CodigosEntidadServicio
{
    public static string ParaRegistro(TipoServicioPublico tipo) => tipo switch
    {
        TipoServicioPublico.Universidad => "UNIVERSIDAD",
        TipoServicioPublico.Telefonia => "TELEFONO",
        TipoServicioPublico.EnergiaElectrica => "LUZ",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de servicio no soportado.")
    };
}
