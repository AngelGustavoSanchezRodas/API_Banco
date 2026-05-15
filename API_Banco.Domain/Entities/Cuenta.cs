using System;

namespace API_Banco.Domain.Entities
{
    public class Cuenta
    {
        public int IdCuenta { get; set; }
        public required string NoCuenta { get; set; }
        public decimal Saldo { get; set; }
        public int IdCliente { get; set; }
        public int IdTipoCuenta { get; set; }
        public int IdEstado { get; set; } = 3;

        public virtual Cliente? Cliente { get; set; }
        public virtual TipoCuenta? TipoCuenta { get; set; }
        public virtual Estado? Estado { get; set; }
        public virtual TarjetaDebito? Tarjeta { get; set; } 
        public virtual ICollection<TransaccionBanco> Transacciones { get; set; } = new List<TransaccionBanco>();

        public void Debitar(decimal monto)
        {
            if (IdEstado != 1) // Asumiendo 1 es Activa
                throw new InvalidOperationException("La cuenta no está activa.");

            if (monto <= 0)
                throw new ArgumentException("El monto a debitar debe ser mayor a cero.");

            if (Saldo < monto)
                throw new InvalidOperationException("Saldo insuficiente.");

            Saldo -= monto;
        }

        public void Acreditar(decimal monto)
        {
            if (IdEstado != 1) // Asumiendo 1 es Activa
                throw new InvalidOperationException("La cuenta no está activa.");

            if (monto <= 0)
                throw new ArgumentException("El monto a depositar debe ser mayor a cero.");

            Saldo += monto;
        }
    }
}
