using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Abstraction
{
    public abstract class NamedEntityMetadata : BaseEntityMetadata
    {
        public required string Nombre { get; set; }
    }
}
