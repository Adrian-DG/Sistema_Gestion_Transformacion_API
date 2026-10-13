using Domain.Entities.Misc;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infraestructure.Identity.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        public required string Identificacion { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public SexoEnum Sexo { get; set; }
        public InstitucionEnum Institucion { get; set; }

        /// <summary>Solo aplica a usuarios militares.</summary>
        public Guid? RangoId { get; set; }
        public virtual Rango? Rango { get; set; }

        /// <summary>Un usuario inactivo no puede iniciar sesión.</summary>
        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }
    }
}
