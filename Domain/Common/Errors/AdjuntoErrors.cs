namespace Domain.Common.Errors;

public static class AdjuntoErrors
{
    public static Error TipoNoPermitido => Error.Validation(
        "Adjunto.TipoNoPermitido",
        "Uno de los documentos adjuntos es de un tipo que no corresponde a esta operación.");
}
