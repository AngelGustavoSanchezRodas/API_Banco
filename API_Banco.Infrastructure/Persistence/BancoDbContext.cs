using API_Banco.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace API_Banco.Infrastructure.Persistence
{
    public class BancoDbContext : DbContext
    {
        public BancoDbContext(DbContextOptions<BancoDbContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<TipoCuenta> TiposCuenta { get; set; }
        public DbSet<TarjetaDebito> TarjetasDebito { get; set; }
        public DbSet<TarjetaCredito> TarjetasCredito { get; set; }
        public DbSet<TransaccionBanco> TransaccionesBanco { get; set; }
        public DbSet<RegistroPagoServicio> RegistroPagosServicios { get; set; }
        public DbSet<CuentaComision> CuentasComision { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<TipoTransaccion> TiposTransaccion { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("cliente");
                entity.HasKey(e => e.IdCliente);

                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.Dpi).HasColumnName("dpi");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Apellido).HasColumnName("apellido");
                entity.Property(e => e.Telefono).HasColumnName("telefono");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Ignore(e => e.Celular);

                entity.HasIndex(e => e.Dpi).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<TipoCuenta>(entity =>
            {
                entity.ToTable("tipo_cuenta");
                entity.HasKey(e => e.IdTipoCuenta);

                entity.Property(e => e.IdTipoCuenta).HasColumnName("id_tipo_cuenta");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            });

            modelBuilder.Entity<Cuenta>(entity =>
            {
                entity.ToTable("cuenta_bancaria");
                entity.HasKey(e => e.IdCuenta);

                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.NoCuenta).HasColumnName("no_cuenta");
                entity.Property(e => e.Saldo).HasColumnName("saldo_actual");
                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.IdTipoCuenta).HasColumnName("id_tipo_cuenta");
                entity.Property(e => e.IdEstado).HasColumnName("id_estado");

                entity.HasIndex(e => e.NoCuenta).IsUnique();

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Cuentas)
                    .HasForeignKey(e => e.IdCliente);

                entity.HasOne(e => e.TipoCuenta)
                    .WithMany(t => t.Cuentas)
                    .HasForeignKey(e => e.IdTipoCuenta);

                entity.HasOne(e => e.Estado)
                    .WithMany()
                    .HasForeignKey(e => e.IdEstado);
            });

            modelBuilder.Entity<TarjetaDebito>(entity =>
            {
                entity.ToTable("tarjeta_debito");
                entity.HasKey(e => e.IdTarjeta);

                entity.Property(e => e.IdTarjeta).HasColumnName("id_tarjeta");
                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.NumeroTarjeta).HasColumnName("no_tarjeta");
                entity.Property(e => e.PinHash).HasColumnName("pin_hash");
                entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
                entity.Property(e => e.IdEstado).HasColumnName("id_estado");

                entity.HasIndex(e => e.NumeroTarjeta).IsUnique();

                entity.HasOne(e => e.Cuenta)
                    .WithOne(c => c.Tarjeta)
                    .HasForeignKey<TarjetaDebito>(e => e.IdCuenta);

                entity.HasOne(e => e.Estado)
                    .WithMany()
                    .HasForeignKey(e => e.IdEstado);
            });

            modelBuilder.Entity<TarjetaCredito>(entity =>
            {
                entity.ToTable("tarjeta_credito");
                entity.HasKey(e => e.IdTarjetaCredito);

                entity.Property(e => e.IdTarjetaCredito).HasColumnName("id_tarjeta_credito");
                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.NoTarjeta).HasColumnName("no_tarjeta");
                entity.Property(e => e.PinHash).HasColumnName("pin_hash");
                entity.Property(e => e.LimiteCredito).HasColumnName("limite_credito");
                entity.Property(e => e.SaldoConsumido).HasColumnName("saldo_consumido");
                entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
                entity.Property(e => e.IdEstado).HasColumnName("id_estado");

                entity.HasIndex(e => e.NoTarjeta).IsUnique();

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.TarjetasCredito)
                    .HasForeignKey(e => e.IdCliente);

                entity.HasOne(e => e.Estado)
                    .WithMany()
                    .HasForeignKey(e => e.IdEstado);
            });

            modelBuilder.Entity<TransaccionBanco>(entity =>
            {
                entity.ToTable("bitacora_transacciones");
                entity.HasKey(e => e.IdTransaccion);

                entity.Property(e => e.IdTransaccion).HasColumnName("id_transaccion");
                entity.Property(e => e.Monto).HasColumnName("monto");
                entity.Property(e => e.Fecha).HasColumnName("fecha_transaccion");
                entity.Property(e => e.IdCuenta).HasColumnName("id_cuenta");
                entity.Property(e => e.IdTipoTransaccion).HasColumnName("id_tipo_transaccion");
                entity.Property(e => e.ReferenciaVinculante).HasColumnName("referencia_vinculante");

                entity.HasOne(e => e.Cuenta)
                    .WithMany(c => c.Transacciones)
                    .HasForeignKey(e => e.IdCuenta);

                entity.HasOne(e => e.TipoTransaccion)
                    .WithMany()
                    .HasForeignKey(e => e.IdTipoTransaccion);
            });

            modelBuilder.Entity<Estado>(entity =>
            {
                entity.ToTable("estado");
                entity.HasKey(e => e.IdEstado);

                entity.Property(e => e.IdEstado).HasColumnName("id_estado");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            });

            modelBuilder.Entity<TipoTransaccion>(entity =>
            {
                entity.ToTable("tipo_transaccion");
                entity.HasKey(e => e.IdTipoTransaccion);

                entity.Property(e => e.IdTipoTransaccion).HasColumnName("id_tipo_transaccion");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            });

            modelBuilder.Entity<RegistroPagoServicio>(entity =>
            {
                entity.ToTable("registro_pagos_servicios");
                entity.HasKey(e => e.IdRegistroPago);

                entity.Property(e => e.IdRegistroPago).HasColumnName("id_registro_pago");
                entity.Property(e => e.IdTransaccionOrigen).HasColumnName("id_transaccion_origen");
                entity.Property(e => e.EntidadServicio).HasColumnName("entidad_servicio");
                entity.Property(e => e.IdentificadorServicio).HasColumnName("identificador_servicio");
                entity.Property(e => e.MontoTotalPagado).HasColumnName("monto_total_pagado");
                entity.Property(e => e.MontoEmpresa95).HasColumnName("monto_empresa_95");
                entity.Property(e => e.ComisionBanco5).HasColumnName("comision_banco_5");

                entity.HasOne(e => e.TransaccionOrigen)
                    .WithMany()
                    .HasForeignKey(e => e.IdTransaccionOrigen);
            });

            modelBuilder.Entity<CuentaComision>(entity =>
            {
                entity.ToTable("cuenta_comision_banco");
                entity.HasKey(e => e.IdCuentaComision);

                entity.Property(e => e.IdCuentaComision).HasColumnName("id_comision_cuenta");
                entity.Property(e => e.NombreCuenta).HasColumnName("nombre_cuenta");
                entity.Property(e => e.SaldoAcumulado).HasColumnName("saldo_acumulado");
            });
        }
    }
}
