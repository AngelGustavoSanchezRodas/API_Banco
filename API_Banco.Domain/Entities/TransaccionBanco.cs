using System;
using System.Collections.Generic;
using System.Text;

namespace API_Banco.Domain.Entities
{
    internal class TransaccionBanco
    {
        public int IdTransaccion { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public int IdCuenta { get; set; }
        public int IdTipoTransaccion { get; set; }

        // Navegación
        public virtual Cuenta? Cuenta { get; set; }
        public virtual TipoTransaccion? TipoTransaccion { get; set; }
    }
}
