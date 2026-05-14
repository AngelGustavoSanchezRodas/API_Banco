using Microsoft.EntityFrameworkCore;
using API_Banco.Domain.Entities;

namespace API_Banco.Infrastructure.Persistence
{
    public class BancoDbContext : DbContext
    {
        public BancoDbContext(DbContextOptions<BancoDbContext> options) : base(options)
        {
        }

        // ====================================================================
        // REPRESENTACIÓN DE LAS TABLAS EN C# (DbSets)
        // Usamos 'internal' para respetar la visibilidad del Domain
        // ====================================================================
        internal DbSet<Cliente> Clientes { get; set; }
        internal DbSet<Cuenta> Cuentas { get; set; }
        internal DbSet<TarjetaDebito> TarjetasDebito { get; set; }
        internal DbSet<TransaccionBanco> TransaccionesBanco { get; set; }

        // Nuevas entidades agregadas para soportar la lógica de la capa de Aplicación
        internal DbSet<Estado> Estados { get; set; }
        internal DbSet<TipoTransaccion> TiposTransaccion { get; set; }

        // ====================================================================
        // CONEXIÓN EXACTA CON LOS NOMBRES EN MySQL
        // ====================================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Le decimos a EF Core el nombre exacto de la tabla en MySQL
            modelBuilder.Entity<Cliente>().ToTable("cliente");
            modelBuilder.Entity<Cuenta>().ToTable("cuenta_bancaria");
            modelBuilder.Entity<TarjetaDebito>().ToTable("tarjeta_debito");
            modelBuilder.Entity<TransaccionBanco>().ToTable("bitacora_transacciones");

            // Nombres de tabla para las nuevas entidades
            modelBuilder.Entity<Estado>().ToTable("estado");
            modelBuilder.Entity<TipoTransaccion>().ToTable("tipo_transaccion");
        }
    }
}
