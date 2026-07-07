using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure.Identity.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        public required string Identitficacion { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public SexoEnum Sexo { get; set; }
        public InstitucionEnum Institucion { get; set; }
        public int RangoId { get; set; }
        public virtual Rango? Rango { get; set; }
    }
}
