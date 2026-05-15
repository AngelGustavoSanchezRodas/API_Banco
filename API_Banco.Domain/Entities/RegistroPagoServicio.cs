using API_Banco.Domain.Entities;

public class RegistroPagoServicio
{
    public int IdRegistroPago { get; set; }
    public int IdTransaccionOrigen { get; set; }
    public required string EntidadServicio { get; set; }
    public required string IdentificadorServicio { get; set; }
    public decimal MontoTotalPagado { get; set; }
    public decimal MontoEmpresa95 { get; set; }
    public decimal ComisionBanco5 { get; set; }
    public TransaccionBanco? TransaccionOrigen { get; set; }
}