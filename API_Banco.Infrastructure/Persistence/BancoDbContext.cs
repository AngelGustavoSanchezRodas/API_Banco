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
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<TarjetaDebito> TarjetasDebito { get; set; }
        public DbSet<TransaccionBanco> TransaccionesBanco { get; set; }

        // Nuevas entidades agregadas para soportar la lógica de la capa de Aplicación
        internal DbSet<Estado> Estados { get; set; }
        internal DbSet<TipoTransaccion> TiposTransaccion { get; set; }

        // ====================================================================
        // CONEXIÓN EXACTA CON LOS NOMBRES EN MySQL
        // ====================================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo de tablas
            modelBuilder.Entity<Cliente>().ToTable("cliente");
            modelBuilder.Entity<Cuenta>().ToTable("cuenta_bancaria");
            modelBuilder.Entity<TarjetaDebito>().ToTable("tarjeta_debito");
            modelBuilder.Entity<TransaccionBanco>().ToTable("bitacora_transacciones");
            modelBuilder.Entity<Estado>().ToTable("estado");
            modelBuilder.Entity<TipoTransaccion>().ToTable("tipo_transaccion");

            // ==============================================================
            // 1. CONFIGURACIÓN DE LLAVES PRIMARIAS (Para evitar el próximo error)
            // ==============================================================
            modelBuilder.Entity<Cliente>().HasKey(c => c.IdCliente);
            modelBuilder.Entity<Cuenta>().HasKey(c => c.IdCuenta);
            modelBuilder.Entity<TarjetaDebito>().HasKey(t => t.IdTarjeta);
            modelBuilder.Entity<TransaccionBanco>().HasKey(t => t.IdTransaccion);
            modelBuilder.Entity<Estado>().HasKey(e => e.IdEstado);
            modelBuilder.Entity<TipoTransaccion>().HasKey(t => t.IdTipoTransaccion);

            // ==============================================================
            // 2. FIX: CONFIGURACIÓN DE LA RELACIÓN 1 A 1 
            // ==============================================================
            modelBuilder.Entity<TarjetaDebito>()
                .HasOne(t => t.Cuenta)
                .WithOne(c => c.Tarjeta)
                .HasForeignKey<TarjetaDebito>(t => t.IdCuenta);

            modelBuilder.Entity<TransaccionBanco>(entity =>
            {
                entity.ToTable("bitacora_transacciones"); 
                entity.HasKey(e => e.IdTransaccion);

                entity.Property(e => e.IdTransaccion).HasColumnName("id_transaccion");
                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.Monto).HasColumnName("monto");
                entity.Property(e => e.Fecha).HasColumnName("fecha_transaccion");
                entity.Property(e => e.IdTipoTransaccion).HasColumnName("IdTipoTransaccion"); 

                // FIX: Especificar la relación y la columna real de la llave foránea
                entity.HasOne(d => d.TipoTransaccion)
                      .WithMany(p => p.Transacciones)
                      .HasForeignKey(d => d.IdTipoTransaccion) 
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Cuenta>(entity =>
            {
                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.NoCuenta).HasColumnName("no_cuenta");
                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.Saldo).HasColumnName("saldo_actual");
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            });


        }
    }
}
