namespace Domain.Entities.Misc
{
    /// <summary>
    /// Tipo de documento permitido en un flujo (<see cref="TipoOperacion"/>): al adjuntar un documento
    /// solo se ofrecen y aceptan los tipos asociados a la operación.
    /// </summary>
    public class TipoDocumentoOperacion
    {
        public Guid TipoDocumentoId { get; set; }
        public virtual TipoDocumento? TipoDocumento { get; set; }

        public Guid TipoOperacionId { get; set; }
        public virtual TipoOperacion? TipoOperacion { get; set; }
    }
}
