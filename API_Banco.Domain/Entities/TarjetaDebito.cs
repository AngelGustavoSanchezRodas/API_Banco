using System;
using System.Collections.Generic;
using System.Text;

namespace API_Banco.Domain.Entities
{
    internal class TarjetaDebito
    {
        public int IdTarjeta { get; set; }
        public required string NumeroTarjeta { get; set; }
        public required string Pin { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int IdCuenta { get; set; }
        public int IdEstado { get; set; }
    }
}
