namespace API_Banco.Domain.Entities
{
    public class UsuarioAcceso
    {
        public int IdUsuario { get; set; }

        /// <summary>
        /// FK opcional. Los usuarios con rol <c>CLIENTE</c> SIEMPRE tienen un IdCliente;
        /// los usuarios con rol <c>ADMIN</c> NO están atados a un cuentahabiente y por
        /// tanto este valor es <c>null</c>.
        /// </summary>
        public int? IdCliente { get; set; }
        public required string NombreUsuario { get; set; }
        public required string CorreoElectronico { get; set; }
        public required string PasswordHash { get; set; }
        public required string Rol { get; set; }

        public Cliente? Cliente { get; set; }
    }
}
