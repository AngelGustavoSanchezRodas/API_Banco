namespace API_Banco.Domain.Entities
{
    public class UsuarioAcceso
    {
        public int IdUsuario { get; set; }
        public int IdCliente { get; set; }
        public required string NombreUsuario { get; set; }
        public required string CorreoElectronico { get; set; }
        public required string PasswordHash { get; set; }
        public required string Rol { get; set; }

        public Cliente? Cliente { get; set; }
    }
}
