using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Abstraction
{
    public abstract class PersonEntityMetadata : BaseEntityMetadata
    {
        public required string Identitficacion { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public SexoEnum  Sexo { get; set; }

        public string DatosPersonales => $"{Identitficacion} - {NombreCompleto}";
        public string NombreCompleto => $"{Apellido} {Nombre}";
    }
}
