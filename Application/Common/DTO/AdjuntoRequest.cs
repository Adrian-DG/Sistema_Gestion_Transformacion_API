using Domain.Entities.Historico;
using FluentValidation;

namespace Application.Common.DTO;

public record AdjuntoRequest(Guid TipoDocumentoId, string NombreArchivo, string ContentType, string FileBase64)
{
    public Adjunto ToEntity(Guid tipoOperacionId, Guid identificadorOperacion) => new()
    {
        TipoDocumentoId = TipoDocumentoId,
        TipoOperacionId = tipoOperacionId,
        IdentificadorOperacion = identificadorOperacion,
        NombreArchivo = NombreArchivo,
        ContentType = ContentType,
        File = Convert.FromBase64String(FileBase64),
    };
}

public class AdjuntoRequestValidator : AbstractValidator<AdjuntoRequest>
{
    public AdjuntoRequestValidator()
    {
        RuleFor(a => a.TipoDocumentoId).NotEmpty();
        RuleFor(a => a.NombreArchivo).NotEmpty().MaximumLength(260);
        RuleFor(a => a.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(a => a.FileBase64)
            .NotEmpty()
            .Must(BeValidBase64)
            .WithMessage("El archivo adjunto no tiene un formato Base64 válido.");
    }

    private static bool BeValidBase64(string value)
    {
        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}
