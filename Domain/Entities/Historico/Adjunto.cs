using Domain.Abstraction;
using Domain.Entities.Misc;

namespace Domain.Entities.Historico
{
    /// <summary>
    /// Documento digitalizado. Pertenece a la entidad indicada por <see cref="TipoOperacionId"/>
    /// (ver <see cref="Constants.TiposOperacion"/>) cuyo Id es <see cref="IdentificadorOperacion"/>.
    /// </summary>
    public class Adjunto : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public required byte[] File { get; set; }
        public required string NombreArchivo { get; set; }
        public required string ContentType { get; set; }
        public Guid TipoDocumentoId { get; set; }
        public virtual TipoDocumento? TipoDocumento { get; set; }
        public Guid TipoOperacionId { get; set; }
        public virtual TipoOperacion? TipoOperacion { get; set; }
        public Guid IdentificadorOperacion { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }


        public string ConvertFileToDateUrl()
        {
            return $"data:{ContentType};base64,{Convert.ToBase64String(File)}";
        }
    }
}
