namespace API_Banco.Domain.Entities
{
    public class TarjetaCredito
    {
        public int IdTarjetaCredito { get; set; }
        public int IdCliente { get; set; }
        public required string NoTarjeta { get; set; }
        public required string PinHash { get; set; }
        public decimal LimiteCredito { get; set; }
        public decimal SaldoConsumido { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int IdEstado { get; set; }

        public virtual Cliente? Cliente { get; set; }
        public virtual Estado? Estado { get; set; }
    }
}
