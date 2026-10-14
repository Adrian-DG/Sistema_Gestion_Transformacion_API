namespace Domain.Entities.Configuracion
{
    /// <summary>
    /// Último número emitido de una numeración (ej. tickets de combustible) en un año; cada año
    /// empieza en 1. Se incrementa con una instrucción atómica dentro de la transacción del documento
    /// que lo usa: dos usuarios nunca obtienen el mismo número y un registro fallido no deja huecos.
    /// </summary>
    public class Secuencia
    {
        public required string Codigo { get; set; }
        public int Anio { get; set; }
        public long Ultimo { get; set; }
    }
}
