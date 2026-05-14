namespace API_Banco.Application.Common;

/// <summary>
/// Regla de negocio 95/5: el 95 % del monto se transfiere a la empresa prestadora y el 5 % a la cuenta de comisiones del banco.
/// La comisión se redondea a 2 decimales; el remanente se asigna a la prestadora para que la suma coincida exactamente con el monto total.
/// </summary>
public static class DistribuidorPago95Por5
{
    private const decimal PorcentajeComision = 0.05m;

    /// <summary>
    /// Calcula la distribución del pago entre prestadora y banco.
    /// </summary>
    /// <param name="montoTotal">Monto total debitado al cuentahabiente.</param>
    /// <returns>Monto neto para la prestadora y monto de comisión para el banco.</returns>
    public static (decimal MontoPrestadora, decimal ComisionBanco) Calcular(decimal montoTotal)
    {
        if (montoTotal <= 0)
            throw new ArgumentOutOfRangeException(nameof(montoTotal), "El monto debe ser mayor que cero.");

        var comision = Math.Round(montoTotal * PorcentajeComision, 2, MidpointRounding.AwayFromZero);
        var prestadora = montoTotal - comision;
        return (prestadora, comision);
    }
}
