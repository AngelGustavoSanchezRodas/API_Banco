using System;

namespace API_Banco.Domain.Entities
{
    public class TarjetaDebito // <-- Cambiado a public
    {
        public int IdTarjeta { get; set; }
        public required string NumeroTarjeta { get; set; }
        public required string PinHash { get; set; } // <-- Mejor PinHash que Pin
        public DateTime FechaVencimiento { get; set; }
        public int IdCuenta { get; set; }
        public int IdEstado { get; set; }

        // Navegación (¡Agregado!)
        public virtual Cuenta? Cuenta { get; set; }
        public virtual Estado? Estado { get; set; }
    }
}