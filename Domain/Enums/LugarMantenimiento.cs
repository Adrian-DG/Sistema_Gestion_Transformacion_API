namespace Domain.Enums
{
    /// <summary>
    /// Dónde se atiende la solicitud de taller. Interno: taller de la institución, con mecánico y piezas
    /// del inventario. Externo: taller autorizado (ej. el concesionario, para conservar la garantía).
    /// </summary>
    public enum LugarMantenimiento
    {
        Interno = 1,
        Externo = 2
    }
}
