using System;
using System.Collections.Generic;
using System.Text;

namespace API_Banco.Domain.Entities
{
    public class TipoTransaccion
    {
        public int IdTipoTransaccion { get; set; }
        public required string Descripcion { get; set; }
    }
}
