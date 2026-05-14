using System;

namespace API_Banco.Domain.Entities
{
    public class TarjetaDebito 
    {
        public int IdTarjeta { get; set; }
        public required string NumeroTarjeta { get; set; }
        public required string PinHash { get; set; } 
        public DateTime FechaVencimiento { get; set; }
        public int IdCuenta { get; set; }
        public int IdEstado { get; set; }

        // Navegación 
        public virtual Cuenta? Cuenta { get; set; }
        public virtual Estado? Estado { get; set; }
    }
}