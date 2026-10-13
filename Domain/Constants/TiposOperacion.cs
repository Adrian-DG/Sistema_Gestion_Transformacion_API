namespace Domain.Constants
{
    /// <summary>
    /// Identificadores fijos del catálogo TipoOperacion. Indican a qué entidad pertenece un
    /// <see cref="Entities.Historico.Adjunto"/> (junto con su IdentificadorOperacion).
    /// </summary>
    public static class TiposOperacion
    {
        public static readonly Guid Vehiculo = new("6f1f1d2a-3c1e-4b7a-9a51-0b7c2d9e1a01");
        public static readonly Guid Asignacion = new("6f1f1d2a-3c1e-4b7a-9a51-0b7c2d9e1a02");
        public static readonly Guid SolicitudTicket = new("6f1f1d2a-3c1e-4b7a-9a51-0b7c2d9e1a03");
        public static readonly Guid MovimientoCombustible = new("6f1f1d2a-3c1e-4b7a-9a51-0b7c2d9e1a04");
    }
}
