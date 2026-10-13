using Application.Common;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Configuracion;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Configuracion;

#region View models

public class PlantillaReporteResumenViewModel
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool EsPredeterminada { get; set; }
    public bool Activa { get; set; }
    public List<string> Logos { get; set; } = [];
    public int TitulosPersonalizados { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

public class PlantillaReporteDetalleViewModel
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool EsPredeterminada { get; set; }
    public bool Activa { get; set; }
    public string? TextoEncabezado { get; set; }
    public string? NotaPiePagina { get; set; }
    public string? FirmanteNombre { get; set; }
    public string? FirmanteRango { get; set; }
    public string? FirmanteProfesion { get; set; }
    public string? FirmanteCargo { get; set; }
    public List<LogoPlantillaViewModel> Logos { get; set; } = [];

    /// <summary>Un elemento por cada reporte del sistema, personalizado o con el título predeterminado.</summary>
    public List<TituloReporteViewModel> Titulos { get; set; } = [];
    public DateTime ActualizadoEn { get; set; }
}

public class LogoPlantillaViewModel
{
    public required string Posicion { get; set; }
    public required string NombreArchivo { get; set; }
    public required string ContentType { get; set; }
    public int Tamano { get; set; }
}

public class TituloReporteViewModel
{
    public TipoReportePdf TipoReporte { get; set; }
    public required string Reporte { get; set; }
    public required string Titulo { get; set; }
    public string? Subtitulo { get; set; }
    public bool Personalizado { get; set; }
}

public class TipoReporteViewModel
{
    public TipoReportePdf TipoReporte { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required string TituloPredeterminado { get; set; }
}

/// <summary>
/// Todo lo que un generador de PDF necesita de la plantilla predeterminada para un reporte.
/// </summary>
public class DatosReportePdf
{
    public Guid PlantillaId { get; set; }
    public required string Plantilla { get; set; }
    public TipoReportePdf TipoReporte { get; set; }
    public required string Titulo { get; set; }
    public string? Subtitulo { get; set; }
    public List<string> LineasEncabezado { get; set; } = [];
    public string? NotaPiePagina { get; set; }
    public FirmanteReporte? Firmante { get; set; }
    public List<LogoReporte> Logos { get; set; } = [];
}

public class FirmanteReporte
{
    public required string Nombre { get; set; }
    public string? Rango { get; set; }
    public string? Profesion { get; set; }
    public string? Cargo { get; set; }

    /// <summary>Línea bajo la firma, ej. "Coronel Lic. Juan Pérez".</summary>
    public string NombreCompleto => string.Join(" ", new[] { Rango, Profesion, Nombre }.Where(p => !string.IsNullOrWhiteSpace(p)));
}

public class LogoReporte
{
    public PosicionLogo Posicion { get; set; }
    public required string ContentType { get; set; }

    /// <summary>En JSON se serializa como Base64.</summary>
    public required byte[] Contenido { get; set; }
}

public record LogoPlantillaArchivo(byte[] Contenido, string ContentType, string NombreArchivo);

#endregion

#region Requests

public record TituloReporteRequest(TipoReportePdf TipoReporte, string Titulo, string? Subtitulo);

public interface IDatosPlantillaRequest
{
    string Nombre { get; }
    string? Descripcion { get; }
    string? TextoEncabezado { get; }
    string? NotaPiePagina { get; }
    string? FirmanteNombre { get; }
    string? FirmanteRango { get; }
    string? FirmanteProfesion { get; }
    string? FirmanteCargo { get; }

    /// <summary>Títulos personalizados; los reportes omitidos usan el título predeterminado.</summary>
    List<TituloReporteRequest>? Titulos { get; }
}

public record CreatePlantillaReporteCommand(
    string Nombre,
    string? Descripcion,
    string? TextoEncabezado,
    string? NotaPiePagina,
    string? FirmanteNombre,
    string? FirmanteRango,
    string? FirmanteProfesion,
    string? FirmanteCargo,
    List<TituloReporteRequest>? Titulos
) : IRequest<Result<Guid>>, IDatosPlantillaRequest;

public record UpdatePlantillaReporteCommand(
    Guid Id,
    string Nombre,
    string? Descripcion,
    string? TextoEncabezado,
    string? NotaPiePagina,
    string? FirmanteNombre,
    string? FirmanteRango,
    string? FirmanteProfesion,
    string? FirmanteCargo,
    List<TituloReporteRequest>? Titulos
) : IRequest<Result<PlantillaReporteDetalleViewModel>>, IDatosPlantillaRequest;

/// <summary>Copia textos, firmante, títulos y logos en una plantilla nueva (inactiva y no predeterminada).</summary>
public record DuplicarPlantillaReporteCommand(Guid Id, string Nombre) : IRequest<Result<Guid>>;

public record PredeterminarPlantillaReporteCommand(Guid Id) : IRequest<Result>;

public record CambiarEstadoPlantillaReporteCommand(Guid Id, bool Activa) : IRequest<Result>;

public record ActualizarLogoPlantillaCommand(Guid Id, PosicionLogo Posicion, string NombreArchivo, string FileBase64)
    : IRequest<Result<PlantillaReporteDetalleViewModel>>;

public record EliminarLogoPlantillaCommand(Guid Id, PosicionLogo Posicion) : IRequest<Result<PlantillaReporteDetalleViewModel>>;

public record GetPlantillasReporteQuery : IRequest<Result<IReadOnlyList<PlantillaReporteResumenViewModel>>>;

public record GetPlantillaReporteByIdQuery(Guid Id) : IRequest<Result<PlantillaReporteDetalleViewModel>>;

public record GetLogoPlantillaQuery(Guid Id, PosicionLogo Posicion) : IRequest<Result<LogoPlantillaArchivo>>;

public record GetTiposReporteQuery : IRequest<Result<IReadOnlyList<TipoReporteViewModel>>>;

/// <summary>Datos de la plantilla predeterminada aplicados a un reporte (para generar el PDF).</summary>
public record GetDatosReportePdfQuery(TipoReportePdf TipoReporte) : IRequest<Result<DatosReportePdf>>;

#endregion

#region Validators

public class DatosPlantillaValidator<T> : AbstractValidator<T> where T : IDatosPlantillaRequest
{
    public DatosPlantillaValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(300);
        RuleFor(x => x.TextoEncabezado).MaximumLength(1000);
        RuleFor(x => x.NotaPiePagina).MaximumLength(1000);
        RuleFor(x => x.FirmanteNombre).MaximumLength(150);
        RuleFor(x => x.FirmanteRango).MaximumLength(100);
        RuleFor(x => x.FirmanteProfesion).MaximumLength(100);
        RuleFor(x => x.FirmanteCargo).MaximumLength(150);

        RuleForEach(x => x.Titulos).ChildRules(t =>
        {
            t.RuleFor(x => x.TipoReporte).IsInEnum().WithMessage("El tipo de reporte no existe.");
            t.RuleFor(x => x.Titulo).NotEmpty().MaximumLength(200);
            t.RuleFor(x => x.Subtitulo).MaximumLength(300);
        });

        RuleFor(x => x.Titulos)
            .Must(t => t is null || t.Select(x => x.TipoReporte).Distinct().Count() == t.Count)
            .WithMessage("Cada reporte puede tener un solo título personalizado.");
    }
}

public class CreatePlantillaReporteCommandValidator : DatosPlantillaValidator<CreatePlantillaReporteCommand>;

public class UpdatePlantillaReporteCommandValidator : DatosPlantillaValidator<UpdatePlantillaReporteCommand>
{
    public UpdatePlantillaReporteCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class DuplicarPlantillaReporteCommandValidator : AbstractValidator<DuplicarPlantillaReporteCommand>
{
    public DuplicarPlantillaReporteCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class ActualizarLogoPlantillaCommandValidator : AbstractValidator<ActualizarLogoPlantillaCommand>
{
    public const int TamanoMaximoBytes = 1024 * 1024;

    public ActualizarLogoPlantillaCommandValidator()
    {
        RuleFor(x => x.Posicion).IsInEnum();
        RuleFor(x => x.NombreArchivo).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileBase64)
            .NotEmpty()
            .Must(b => Imagen.Decodificar(b) is not null).WithMessage("El logo no tiene un formato Base64 válido.")
            .Must(b => (Imagen.Decodificar(b)?.Length ?? 0) <= TamanoMaximoBytes).WithMessage("El logo no puede superar 1 MB.");
    }
}

#endregion

#region Handlers

public class PlantillaReporteQueryHandler(IUnitOfWork uow) :
    IRequestHandler<GetPlantillasReporteQuery, Result<IReadOnlyList<PlantillaReporteResumenViewModel>>>,
    IRequestHandler<GetPlantillaReporteByIdQuery, Result<PlantillaReporteDetalleViewModel>>,
    IRequestHandler<GetLogoPlantillaQuery, Result<LogoPlantillaArchivo>>,
    IRequestHandler<GetTiposReporteQuery, Result<IReadOnlyList<TipoReporteViewModel>>>,
    IRequestHandler<GetDatosReportePdfQuery, Result<DatosReportePdf>>
{
    public async Task<Result<IReadOnlyList<PlantillaReporteResumenViewModel>>> Handle(GetPlantillasReporteQuery request, CancellationToken cancellationToken)
    {
        var plantillas = await uow.Query<PlantillaReporte>()
            .OrderByDescending(p => p.EsPredeterminada).ThenBy(p => p.Nombre)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.EsPredeterminada,
                p.Status,
                p.UpdatedAt,
                Logos = p.Logos.OrderBy(l => l.Posicion).Select(l => l.Posicion).ToList(),
                Titulos = p.Titulos.Count
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PlantillaReporteResumenViewModel>>(plantillas.Select(p => new PlantillaReporteResumenViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            EsPredeterminada = p.EsPredeterminada,
            Activa = p.Status,
            Logos = p.Logos.Select(l => l.ToString()).ToList(),
            TitulosPersonalizados = p.Titulos,
            ActualizadoEn = p.UpdatedAt
        }).ToList());
    }

    public Task<Result<PlantillaReporteDetalleViewModel>> Handle(GetPlantillaReporteByIdQuery request, CancellationToken cancellationToken) =>
        PlantillasReporteStore.DetalleAsync(uow, request.Id, cancellationToken);

    public async Task<Result<LogoPlantillaArchivo>> Handle(GetLogoPlantillaQuery request, CancellationToken cancellationToken)
    {
        var logo = await uow.Query<PlantillaReporteLogo>()
            .Where(l => l.PlantillaReporteId == request.Id && l.Posicion == request.Posicion)
            .Select(l => new LogoPlantillaArchivo(l.Contenido, l.ContentType, l.NombreArchivo))
            .FirstOrDefaultAsync(cancellationToken);

        return logo is null
            ? Result.Failure<LogoPlantillaArchivo>(PlantillaReporteErrors.LogoNoEncontrado(request.Posicion))
            : Result.Success(logo);
    }

    public Task<Result<IReadOnlyList<TipoReporteViewModel>>> Handle(GetTiposReporteQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<TipoReporteViewModel> tipos = Enum.GetValues<TipoReportePdf>().Select(t => new TipoReporteViewModel
        {
            TipoReporte = t,
            Codigo = t.ToString(),
            Nombre = TiposReportePdf.Nombre(t),
            TituloPredeterminado = TiposReportePdf.TituloPredeterminado(t)
        }).ToList();

        return Task.FromResult(Result.Success(tipos));
    }

    public async Task<Result<DatosReportePdf>> Handle(GetDatosReportePdfQuery request, CancellationToken cancellationToken)
    {
        var plantilla = await uow.Query<PlantillaReporte>()
            .Include(p => p.Logos)
            .Include(p => p.Titulos.Where(t => t.TipoReporte == request.TipoReporte))
            .FirstOrDefaultAsync(p => p.EsPredeterminada && p.Status, cancellationToken);

        if (plantilla is null)
        {
            return Result.Failure<DatosReportePdf>(PlantillaReporteErrors.SinPredeterminada);
        }

        var titulo = plantilla.Titulos.FirstOrDefault();

        return Result.Success(new DatosReportePdf
        {
            PlantillaId = plantilla.Id,
            Plantilla = plantilla.Nombre,
            TipoReporte = request.TipoReporte,
            Titulo = titulo?.Titulo ?? TiposReportePdf.TituloPredeterminado(request.TipoReporte),
            Subtitulo = titulo?.Subtitulo,
            LineasEncabezado = (plantilla.TextoEncabezado ?? string.Empty)
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList(),
            NotaPiePagina = plantilla.NotaPiePagina,
            Firmante = string.IsNullOrWhiteSpace(plantilla.FirmanteNombre) ? null : new FirmanteReporte
            {
                Nombre = plantilla.FirmanteNombre,
                Rango = plantilla.FirmanteRango,
                Profesion = plantilla.FirmanteProfesion,
                Cargo = plantilla.FirmanteCargo
            },
            Logos = plantilla.Logos
                .OrderBy(l => l.Posicion)
                .Select(l => new LogoReporte { Posicion = l.Posicion, ContentType = l.ContentType, Contenido = l.Contenido })
                .ToList()
        });
    }
}

public class PlantillaReporteCommandHandler(IUnitOfWork uow) :
    IRequestHandler<CreatePlantillaReporteCommand, Result<Guid>>,
    IRequestHandler<UpdatePlantillaReporteCommand, Result<PlantillaReporteDetalleViewModel>>,
    IRequestHandler<DuplicarPlantillaReporteCommand, Result<Guid>>,
    IRequestHandler<PredeterminarPlantillaReporteCommand, Result>,
    IRequestHandler<CambiarEstadoPlantillaReporteCommand, Result>,
    IRequestHandler<ActualizarLogoPlantillaCommand, Result<PlantillaReporteDetalleViewModel>>,
    IRequestHandler<EliminarLogoPlantillaCommand, Result<PlantillaReporteDetalleViewModel>>
{
    public async Task<Result<Guid>> Handle(CreatePlantillaReporteCommand request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre.Trim();
        if (await NombreEnUsoAsync(nombre, null, cancellationToken))
        {
            return Result.Failure<Guid>(PlantillaReporteErrors.NombreDuplicado(nombre));
        }

        // La primera plantilla del sistema queda como predeterminada.
        var hayPredeterminada = await uow.Query<PlantillaReporte>().AnyAsync(p => p.EsPredeterminada, cancellationToken);

        var plantilla = new PlantillaReporte { Nombre = nombre, Status = true, EsPredeterminada = !hayPredeterminada };
        Aplicar(plantilla, request);

        await uow.AddAsync(plantilla, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(plantilla.Id);
    }

    public async Task<Result<PlantillaReporteDetalleViewModel>> Handle(UpdatePlantillaReporteCommand request, CancellationToken cancellationToken)
    {
        var plantilla = await uow.Tracked<PlantillaReporte>()
            .Include(p => p.Titulos)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (plantilla is null)
        {
            return Result.Failure<PlantillaReporteDetalleViewModel>(PlantillaReporteErrors.NotFound(request.Id));
        }

        var nombre = request.Nombre.Trim();
        if (await NombreEnUsoAsync(nombre, request.Id, cancellationToken))
        {
            return Result.Failure<PlantillaReporteDetalleViewModel>(PlantillaReporteErrors.NombreDuplicado(nombre));
        }

        plantilla.Nombre = nombre;
        Aplicar(plantilla, request);

        await uow.SaveChangesAsync(cancellationToken);
        return await PlantillasReporteStore.DetalleAsync(uow, plantilla.Id, cancellationToken);
    }

    public async Task<Result<Guid>> Handle(DuplicarPlantillaReporteCommand request, CancellationToken cancellationToken)
    {
        var origen = await uow.Query<PlantillaReporte>()
            .Include(p => p.Logos)
            .Include(p => p.Titulos)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (origen is null) return Result.Failure<Guid>(PlantillaReporteErrors.NotFound(request.Id));

        var nombre = request.Nombre.Trim();
        if (await NombreEnUsoAsync(nombre, null, cancellationToken))
        {
            return Result.Failure<Guid>(PlantillaReporteErrors.NombreDuplicado(nombre));
        }

        var copia = new PlantillaReporte
        {
            Nombre = nombre,
            Descripcion = origen.Descripcion,
            TextoEncabezado = origen.TextoEncabezado,
            NotaPiePagina = origen.NotaPiePagina,
            FirmanteNombre = origen.FirmanteNombre,
            FirmanteRango = origen.FirmanteRango,
            FirmanteProfesion = origen.FirmanteProfesion,
            FirmanteCargo = origen.FirmanteCargo,
            Status = false,
            EsPredeterminada = false,
            Logos = origen.Logos.Select(l => new PlantillaReporteLogo
            {
                Posicion = l.Posicion,
                Contenido = l.Contenido,
                ContentType = l.ContentType,
                NombreArchivo = l.NombreArchivo,
                Status = true
            }).ToList(),
            Titulos = origen.Titulos.Select(t => new PlantillaReporteTitulo
            {
                TipoReporte = t.TipoReporte,
                Titulo = t.Titulo,
                Subtitulo = t.Subtitulo,
                Status = true
            }).ToList()
        };

        await uow.AddAsync(copia, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(copia.Id);
    }

    public async Task<Result> Handle(PredeterminarPlantillaReporteCommand request, CancellationToken cancellationToken)
    {
        var plantilla = await uow.Tracked<PlantillaReporte>().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plantilla is null) return Result.Failure(PlantillaReporteErrors.NotFound(request.Id));
        if (plantilla.EsPredeterminada) return Result.Success();
        if (!plantilla.Status) return Result.Failure(PlantillaReporteErrors.InactivaNoPredeterminable);

        // Dos guardados: el índice único filtrado no admite dos predeterminadas ni por un instante.
        return await uow.EnTransaccionAsync(async () =>
        {
            var anteriores = await uow.Tracked<PlantillaReporte>().Where(p => p.EsPredeterminada).ToListAsync(cancellationToken);
            anteriores.ForEach(p => p.EsPredeterminada = false);
            await uow.SaveChangesAsync(cancellationToken);

            plantilla.EsPredeterminada = true;
            await uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }

    public async Task<Result> Handle(CambiarEstadoPlantillaReporteCommand request, CancellationToken cancellationToken)
    {
        var plantilla = await uow.Tracked<PlantillaReporte>().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plantilla is null) return Result.Failure(PlantillaReporteErrors.NotFound(request.Id));
        if (plantilla.Status == request.Activa) return Result.Success();
        if (!request.Activa && plantilla.EsPredeterminada) return Result.Failure(PlantillaReporteErrors.PredeterminadaNoDesactivable);

        plantilla.Status = request.Activa;
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PlantillaReporteDetalleViewModel>> Handle(ActualizarLogoPlantillaCommand request, CancellationToken cancellationToken)
    {
        var contenido = Imagen.Decodificar(request.FileBase64)!;
        var contentType = Imagen.DetectarContentType(contenido);

        // Solo PNG y JPEG: son los formatos que admiten todos los generadores de PDF.
        if (contentType is not (Imagen.Png or Imagen.Jpeg))
        {
            return Result.Failure<PlantillaReporteDetalleViewModel>(PlantillaReporteErrors.FormatoLogoInvalido);
        }

        if (!await uow.Query<PlantillaReporte>().AnyAsync(p => p.Id == request.Id, cancellationToken))
        {
            return Result.Failure<PlantillaReporteDetalleViewModel>(PlantillaReporteErrors.NotFound(request.Id));
        }

        var logo = await uow.Tracked<PlantillaReporteLogo>()
            .FirstOrDefaultAsync(l => l.PlantillaReporteId == request.Id && l.Posicion == request.Posicion, cancellationToken);

        var nombreArchivo = Path.GetFileName(request.NombreArchivo.Trim());
        if (logo is null)
        {
            await uow.AddAsync(new PlantillaReporteLogo
            {
                PlantillaReporteId = request.Id,
                Posicion = request.Posicion,
                Contenido = contenido,
                ContentType = contentType,
                NombreArchivo = nombreArchivo,
                Status = true
            }, cancellationToken);
        }
        else
        {
            logo.Contenido = contenido;
            logo.ContentType = contentType;
            logo.NombreArchivo = nombreArchivo;
        }

        await TocarAsync(request.Id, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return await PlantillasReporteStore.DetalleAsync(uow, request.Id, cancellationToken);
    }

    public async Task<Result<PlantillaReporteDetalleViewModel>> Handle(EliminarLogoPlantillaCommand request, CancellationToken cancellationToken)
    {
        if (!await uow.Query<PlantillaReporte>().AnyAsync(p => p.Id == request.Id, cancellationToken))
        {
            return Result.Failure<PlantillaReporteDetalleViewModel>(PlantillaReporteErrors.NotFound(request.Id));
        }

        var logo = await uow.Tracked<PlantillaReporteLogo>()
            .FirstOrDefaultAsync(l => l.PlantillaReporteId == request.Id && l.Posicion == request.Posicion, cancellationToken);

        if (logo is not null)
        {
            uow.Remove(logo);
            await TocarAsync(request.Id, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
        }

        return await PlantillasReporteStore.DetalleAsync(uow, request.Id, cancellationToken);
    }

    private Task<bool> NombreEnUsoAsync(string nombre, Guid? excluirId, CancellationToken cancellationToken) =>
        uow.Query<PlantillaReporte>().AnyAsync(p => p.Nombre == nombre && p.Id != excluirId, cancellationToken);

    /// <summary>Marca la plantilla como modificada (fecha y usuario de auditoría) al cambiar un logo.</summary>
    private async Task TocarAsync(Guid id, CancellationToken cancellationToken)
    {
        var plantilla = await uow.Tracked<PlantillaReporte>().FirstAsync(p => p.Id == id, cancellationToken);
        plantilla.UpdatedAt = DateTime.UtcNow;
    }

    private void Aplicar(PlantillaReporte plantilla, IDatosPlantillaRequest datos)
    {
        plantilla.Descripcion = Limpiar(datos.Descripcion);
        plantilla.TextoEncabezado = Limpiar(datos.TextoEncabezado?.Replace("\r\n", "\n"));
        plantilla.NotaPiePagina = Limpiar(datos.NotaPiePagina);
        plantilla.FirmanteNombre = Limpiar(datos.FirmanteNombre);
        plantilla.FirmanteRango = Limpiar(datos.FirmanteRango);
        plantilla.FirmanteProfesion = Limpiar(datos.FirmanteProfesion);
        plantilla.FirmanteCargo = Limpiar(datos.FirmanteCargo);

        var solicitados = (datos.Titulos ?? []).ToDictionary(t => t.TipoReporte);

        foreach (var existente in plantilla.Titulos.ToList())
        {
            if (solicitados.Remove(existente.TipoReporte, out var nuevo))
            {
                existente.Titulo = nuevo.Titulo.Trim();
                existente.Subtitulo = Limpiar(nuevo.Subtitulo);
            }
            else
            {
                plantilla.Titulos.Remove(existente);
                uow.Remove(existente);
            }
        }

        foreach (var nuevo in solicitados.Values)
        {
            plantilla.Titulos.Add(new PlantillaReporteTitulo
            {
                TipoReporte = nuevo.TipoReporte,
                Titulo = nuevo.Titulo.Trim(),
                Subtitulo = Limpiar(nuevo.Subtitulo),
                Status = true
            });
        }
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

internal static class PlantillasReporteStore
{
    public static async Task<Result<PlantillaReporteDetalleViewModel>> DetalleAsync(IUnitOfWork uow, Guid id, CancellationToken cancellationToken)
    {
        var plantilla = await uow.Query<PlantillaReporte>()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.EsPredeterminada,
                p.Status,
                p.TextoEncabezado,
                p.NotaPiePagina,
                p.FirmanteNombre,
                p.FirmanteRango,
                p.FirmanteProfesion,
                p.FirmanteCargo,
                p.UpdatedAt,
                // Sin el contenido de las imágenes: solo sus datos.
                Logos = p.Logos.OrderBy(l => l.Posicion)
                    .Select(l => new { l.Posicion, l.NombreArchivo, l.ContentType, Tamano = l.Contenido.Length })
                    .ToList(),
                Titulos = p.Titulos.Select(t => new { t.TipoReporte, t.Titulo, t.Subtitulo }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (plantilla is null)
        {
            return Result.Failure<PlantillaReporteDetalleViewModel>(PlantillaReporteErrors.NotFound(id));
        }

        return Result.Success(new PlantillaReporteDetalleViewModel
        {
            Id = plantilla.Id,
            Nombre = plantilla.Nombre,
            Descripcion = plantilla.Descripcion,
            EsPredeterminada = plantilla.EsPredeterminada,
            Activa = plantilla.Status,
            TextoEncabezado = plantilla.TextoEncabezado,
            NotaPiePagina = plantilla.NotaPiePagina,
            FirmanteNombre = plantilla.FirmanteNombre,
            FirmanteRango = plantilla.FirmanteRango,
            FirmanteProfesion = plantilla.FirmanteProfesion,
            FirmanteCargo = plantilla.FirmanteCargo,
            ActualizadoEn = plantilla.UpdatedAt,
            Logos = plantilla.Logos.Select(l => new LogoPlantillaViewModel
            {
                Posicion = l.Posicion.ToString(),
                NombreArchivo = l.NombreArchivo,
                ContentType = l.ContentType,
                Tamano = l.Tamano
            }).ToList(),
            Titulos = Enum.GetValues<TipoReportePdf>().Select(tipo =>
            {
                var personalizado = plantilla.Titulos.FirstOrDefault(t => t.TipoReporte == tipo);
                return new TituloReporteViewModel
                {
                    TipoReporte = tipo,
                    Reporte = TiposReportePdf.Nombre(tipo),
                    Titulo = personalizado?.Titulo ?? TiposReportePdf.TituloPredeterminado(tipo),
                    Subtitulo = personalizado?.Subtitulo,
                    Personalizado = personalizado is not null
                };
            }).ToList()
        });
    }
}

#endregion
