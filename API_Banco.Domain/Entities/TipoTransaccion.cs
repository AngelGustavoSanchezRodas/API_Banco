using System;
using System.Collections.Generic;
using System.Text;

namespace API_Banco.Domain.Entities
{
    internal class TipoTransaccion
    {
        public int IdTipoTransaccion { get; set; }
        public required string Descripcion { get; set; }
    }
}
