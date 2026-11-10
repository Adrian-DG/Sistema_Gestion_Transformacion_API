namespace Domain.Enums
{
    /// <summary>
    /// Tipo de proveedor. Taller: taller autorizado (ej. concesionario) para mantenimientos externos y
    /// garantías. SuplidorCombustible: el único que se elige al registrar una entrada de combustible.
    /// Otro: suplidor de piezas u otros servicios. Las compras de piezas aceptan cualquier tipo.
    /// </summary>
    public enum TipoProveedor
    {
        Otro = 0,
        Taller = 1,
        SuplidorCombustible = 2
    }
}
