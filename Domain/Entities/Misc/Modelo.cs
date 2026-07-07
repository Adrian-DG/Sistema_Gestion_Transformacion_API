using Domain.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Misc
{
    public class Modelo : NamedEntityMetadata
    {
        public Guid MarcaId { get; set; }
        public virtual Marca? Marca { get; set; }
    }
}
