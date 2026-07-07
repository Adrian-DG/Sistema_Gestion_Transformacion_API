using Domain.Abstraction;
using Domain.Entities.Misc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Historico
{
    public class Adjunto : BaseEntityMetadata, IAuditableEntityMetadata
    {
        public required byte[] File { get; set; }
        public Guid TipoDocumentoId { get; set; }
        public virtual TipoDocumento? TipoDocumento { get; set; }
        public Guid TipoOperacionId { get; set; }
        public virtual TipoOperacion? TipoOperacion { get; set; }
        public Guid IdentificadorOperacion { get; set; }

        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }

        public string ConvertFileToDateUrl()
        {
            return $"data:application/octet-stream;base64,{Convert.ToBase64String(File)}";
        }
    }
}
