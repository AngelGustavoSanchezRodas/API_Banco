namespace API_Banco.Domain.Entities
{
    public class TipoCuenta
    {
        public int IdTipoCuenta { get; set; }
        public required string Descripcion { get; set; }

        public virtual ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    }
}
