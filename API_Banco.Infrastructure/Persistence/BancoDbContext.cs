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

            // 1. Configuración de CLIENTE
            modelBuilder.Entity<Cliente>(entity => {
                entity.ToTable("cliente");
                entity.HasKey(e => e.IdCliente);

                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.Dpi).HasColumnName("dpi");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Apellido).HasColumnName("apellido");

                
                entity.Property(e => e.Celular).HasColumnName("telefono");

                entity.Property(e => e.Email).HasColumnName("email");
            });

            // 2. Configuración de CUENTA BANCARIA
            modelBuilder.Entity<Cuenta>(entity => {
                entity.ToTable("cuenta_bancaria"); //
                entity.HasKey(e => e.IdCuenta); //
                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.NoCuenta).HasColumnName("no_cuenta");
                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.Saldo).HasColumnName("saldo_actual");
                entity.Property(e => e.IdEstado).HasColumnName("IdEstado");
            });

            // 3. Configuración de TARJETA DE DEBITO
            modelBuilder.Entity<TarjetaDebito>(entity => {
                entity.ToTable("tarjeta_debito"); //
                entity.HasKey(e => e.IdTarjeta); //
                entity.Property(e => e.IdTarjeta).HasColumnName("id_tarjeta");
                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.NumeroTarjeta).HasColumnName("no_tarjeta");
                entity.Property(e => e.PinHash).HasColumnName("pin_hash");
                entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
                entity.Property(e => e.IdEstado).HasColumnName("IdEstado");

                // Relación 1 a 1 con Cuenta
                entity.HasOne(t => t.Cuenta)
                      .WithOne(c => c.Tarjeta)
                      .HasForeignKey<TarjetaDebito>(t => t.IdCuenta);
            });

            // 4. Configuración de BITÁCORA (Transacciones)
            modelBuilder.Entity<TransaccionBanco>(entity => {
                entity.ToTable("bitacora_transacciones"); //
                entity.HasKey(e => e.IdTransaccion); //
                entity.Property(e => e.IdTransaccion).HasColumnName("id_transaccion");
                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.Monto).HasColumnName("monto");
                entity.Property(e => e.Fecha).HasColumnName("fecha_transaccion");
                entity.Property(e => e.IdTipoTransaccion).HasColumnName("IdTipoTransaccion");

                entity.HasOne(t => t.TipoTransaccion)
                      .WithMany()
                      .HasForeignKey(t => t.IdTipoTransaccion);
            });

            // 5. Catálogos nuevos
            modelBuilder.Entity<Estado>().ToTable("estado").HasKey(e => e.IdEstado); //
            modelBuilder.Entity<TipoTransaccion>().ToTable("tipo_transaccion").HasKey(t => t.IdTipoTransaccion); //
        }
    }
}
