namespace API_Banco.Domain.Entities
{
    public class CuentaComision
    {
        public int IdCuentaComision { get; set; }
        public required string NombreCuenta { get; set; } 
        public decimal SaldoAcumulado { get; set; }
    }
}