using System;

namespace API_Banco.Domain.Entities
{
    public class RegistroPagoServicio
    {
        public int IdRegistroPago { get; set; }
        public int IdTransaccionOrigen { get; set; }
        public string EntidadServicio { get; set; }
        public string IdentificadorServicio { get; set; }
        public decimal MontoPagado { get; set; }

        public TransaccionBanco TransaccionOrigen { get; set; }
    }
}
