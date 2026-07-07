using Domain.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Misc
{
    public class Rango : NamedEntityMetadata
    {
        public required string NombreArmada { get; set; }
    }
}
