namespace API_Banco.Domain.Entities
{
    public class Cuenta
    {
        public int IdCuenta { get; set; }
        public required string NoCuenta { get; set; }
        public decimal Saldo { get; set; }
        public int IdCliente { get; set; }
        public int IdEstado { get; set; }

        public virtual Cliente? Cliente { get; set; }
        public virtual Estado? Estado { get; set; }
        public virtual TarjetaDebito? Tarjeta { get; set; } 
        public virtual ICollection<TransaccionBanco> Transacciones { get; set; } = new List<TransaccionBanco>();
    }
}