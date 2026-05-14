namespace API_Banco.Domain.Entities
{
    public class Cliente
    {
        public int IdCliente { get; set; }
        public required string Dpi { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public string? Celular { get; set; }
        public string? Email { get; set; }

        public virtual ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    }
}
