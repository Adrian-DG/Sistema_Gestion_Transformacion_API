using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Recursos;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Vehiculo;

/// <summary>Fotografía de una vista del vehículo (JPEG, PNG o WebP de hasta 5 MB), en Base64 sin prefijo.</summary>
public record FotoVehiculoRequest(VistaVehiculo Vista, string NombreArchivo, string ContentType, string FileBase64)
{
    public FotoVehiculo ToEntity(Guid vehiculoId) => new()
    {
        VehiculoId = vehiculoId,
        Vista = Vista,
        NombreArchivo = NombreArchivo.Trim(),
        ContentType = ContentType,
        File = Convert.FromBase64String(FileBase64),
        Status = true
    };
}

public class FotoVehiculoRequestValidator : AbstractValidator<FotoVehiculoRequest>
{
    public const int MaximoBytes = 5 * 1024 * 1024;
    private static readonly string[] TiposPermitidos = ["image/jpeg", "image/png", "image/webp"];

    public FotoVehiculoRequestValidator()
    {
        RuleFor(f => f.Vista).IsInEnum();
        RuleFor(f => f.NombreArchivo).NotEmpty().MaximumLength(260);
        RuleFor(f => f.ContentType)
            .Must(t => TiposPermitidos.Contains(t))
            .WithMessage("La foto debe ser una imagen JPEG, PNG o WebP.");
        RuleFor(f => f.FileBase64)
            .NotEmpty()
            .Must(EsBase64).WithMessage("La foto no tiene un formato Base64 válido.")
            .Must(b => b.Length / 4L * 3 <= MaximoBytes).WithMessage("Cada foto debe pesar como máximo 5 MB.");
    }

    private static bool EsBase64(string valor) => Convert.TryFromBase64String(valor, new byte[valor.Length], out _);
}

public class FotoVehiculoViewModel
{
    public Guid Id { get; set; }
    public VistaVehiculo Vista { get; set; }
    public required string NombreArchivo { get; set; }
    public DateTime Fecha { get; set; }
}

public record ArchivoFotoVehiculo(byte[] Contenido, string ContentType, string NombreArchivo);

/// <summary>Fotos vigentes del vehículo (una por vista).</summary>
public record GetFotosVehiculoQuery(Guid VehiculoId) : IRequest<Result<IReadOnlyList<FotoVehiculoViewModel>>>;

public record GetFotoVehiculoQuery(Guid VehiculoId, VistaVehiculo Vista) : IRequest<Result<ArchivoFotoVehiculo>>;

/// <summary>Reemplaza la foto de una vista; la anterior queda inactiva como historial.</summary>
public record ReemplazarFotoVehiculoCommand(Guid VehiculoId, FotoVehiculoRequest Foto) : IRequest<Result>;

public class ReemplazarFotoVehiculoCommandValidator : AbstractValidator<ReemplazarFotoVehiculoCommand>
{
    public ReemplazarFotoVehiculoCommandValidator() =>
        RuleFor(x => x.Foto).NotNull().SetValidator(new FotoVehiculoRequestValidator());
}

public class FotosVehiculoHandler(IUnitOfWork uow) :
    IRequestHandler<GetFotosVehiculoQuery, Result<IReadOnlyList<FotoVehiculoViewModel>>>,
    IRequestHandler<GetFotoVehiculoQuery, Result<ArchivoFotoVehiculo>>,
    IRequestHandler<ReemplazarFotoVehiculoCommand, Result>
{
    public async Task<Result<IReadOnlyList<FotoVehiculoViewModel>>> Handle(GetFotosVehiculoQuery request, CancellationToken cancellationToken)
    {
        if (!await uow.Query<Domain.Entities.Recursos.Vehiculo>().AnyAsync(v => v.Id == request.VehiculoId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<FotoVehiculoViewModel>>(VehiculoErrors.NotFound(request.VehiculoId));
        }

        var fotos = await uow.Query<FotoVehiculo>()
            .Where(f => f.VehiculoId == request.VehiculoId && f.Status)
            .OrderBy(f => f.Vista)
            .Select(f => new FotoVehiculoViewModel { Id = f.Id, Vista = f.Vista, NombreArchivo = f.NombreArchivo, Fecha = f.CreatedAt })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<FotoVehiculoViewModel>>(fotos);
    }

    public async Task<Result<ArchivoFotoVehiculo>> Handle(GetFotoVehiculoQuery request, CancellationToken cancellationToken)
    {
        var foto = await uow.Query<FotoVehiculo>()
            .Where(f => f.VehiculoId == request.VehiculoId && f.Vista == request.Vista && f.Status)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new ArchivoFotoVehiculo(f.File, f.ContentType, f.NombreArchivo))
            .FirstOrDefaultAsync(cancellationToken);

        return foto is null
            ? Result.Failure<ArchivoFotoVehiculo>(VehiculoErrors.FotoNotFound(request.VehiculoId, request.Vista))
            : Result.Success(foto);
    }

    public async Task<Result> Handle(ReemplazarFotoVehiculoCommand request, CancellationToken cancellationToken)
    {
        if (!await uow.Query<Domain.Entities.Recursos.Vehiculo>().AnyAsync(v => v.Id == request.VehiculoId, cancellationToken))
        {
            return Result.Failure(VehiculoErrors.NotFound(request.VehiculoId));
        }

        var anteriores = await uow.Tracked<FotoVehiculo>()
            .Where(f => f.VehiculoId == request.VehiculoId && f.Vista == request.Foto.Vista && f.Status)
            .ToListAsync(cancellationToken);
        foreach (var anterior in anteriores) anterior.Status = false;

        await uow.AddAsync(request.Foto.ToEntity(request.VehiculoId), cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
