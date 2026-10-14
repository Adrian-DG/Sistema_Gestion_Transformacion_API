using Application.Common;
using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Configuracion;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Application.Features.Configuracion;

#region View models

/// <summary>Apariencia activa, la que aplica el frontend (lectura pública).</summary>
public class AparienciaViewModel
{
    /// <summary>Cambia al activar otra apariencia; junto con <see cref="Version"/> identifica el logo en caché.</summary>
    public Guid Id { get; set; }
    public required string NombreSistema { get; set; }
    public string? NombreCorto { get; set; }
    public string? NombreInstitucion { get; set; }
    public bool TieneLogo { get; set; }
    public required string ColorPrimario { get; set; }
    public required string ColorSecundario { get; set; }
    public required string ColorBotonPrimario { get; set; }
    public required string ColorBotonSecundario { get; set; }
    public required string ColorBotonPeligro { get; set; }

    /// <summary>Aumenta con cada cambio; el frontend la compara con la que tiene guardada.</summary>
    public int Version { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Apariencia guardada, para el listado y el formulario de administración.</summary>
public class AparienciaDetalleViewModel
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool EsActiva { get; set; }
    public required string NombreSistema { get; set; }
    public string? NombreCorto { get; set; }
    public string? NombreInstitucion { get; set; }
    public bool TieneLogo { get; set; }
    public string? LogoNombreArchivo { get; set; }
    public required string ColorPrimario { get; set; }
    public required string ColorSecundario { get; set; }
    public required string ColorBotonPrimario { get; set; }
    public required string ColorBotonSecundario { get; set; }
    public required string ColorBotonPeligro { get; set; }
    public int Version { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

public record LogoArchivo(byte[] Contenido, string ContentType, Guid AparienciaId, int Version);

#endregion

#region Requests

public interface IDatosApariencia
{
    string Nombre { get; }
    string? Descripcion { get; }
    string NombreSistema { get; }
    string? NombreCorto { get; }
    string? NombreInstitucion { get; }
    string ColorPrimario { get; }
    string ColorSecundario { get; }
    string ColorBotonPrimario { get; }
    string ColorBotonSecundario { get; }
    string ColorBotonPeligro { get; }
}

public record GetAparienciaQuery : IRequest<Result<AparienciaViewModel>>;

/// <summary>Logo de la apariencia activa (lectura pública).</summary>
public record GetLogoQuery : IRequest<Result<LogoArchivo>>;

public record GetAparienciasQuery : IRequest<Result<IReadOnlyList<AparienciaDetalleViewModel>>>;

public record GetAparienciaByIdQuery(Guid Id) : IRequest<Result<AparienciaDetalleViewModel>>;

/// <summary>Logo de cualquier apariencia guardada, para su vista previa.</summary>
public record GetLogoAparienciaQuery(Guid Id) : IRequest<Result<LogoArchivo>>;

/// <summary>Crea una apariencia inactiva; se aplica al activarla.</summary>
public record CreateAparienciaCommand(
    string Nombre,
    string? Descripcion,
    string NombreSistema,
    string? NombreCorto,
    string? NombreInstitucion,
    string ColorPrimario,
    string ColorSecundario,
    string ColorBotonPrimario,
    string ColorBotonSecundario,
    string ColorBotonPeligro
) : IRequest<Result<Guid>>, IDatosApariencia;

public record UpdateAparienciaCommand(
    Guid Id,
    string Nombre,
    string? Descripcion,
    string NombreSistema,
    string? NombreCorto,
    string? NombreInstitucion,
    string ColorPrimario,
    string ColorSecundario,
    string ColorBotonPrimario,
    string ColorBotonSecundario,
    string ColorBotonPeligro
) : IRequest<Result<AparienciaDetalleViewModel>>, IDatosApariencia;

/// <summary>Copia textos, colores y logo en una apariencia nueva (inactiva).</summary>
public record DuplicarAparienciaCommand(Guid Id, string Nombre) : IRequest<Result<Guid>>;

/// <summary>Hace que todos los usuarios vean esta apariencia; la que estaba activa deja de estarlo.</summary>
public record ActivarAparienciaCommand(Guid Id) : IRequest<Result>;

/// <param name="FileBase64">Imagen PNG, JPEG o WebP. El tipo se determina por el contenido, no por el nombre.</param>
public record ActualizarLogoCommand(Guid Id, string NombreArchivo, string FileBase64) : IRequest<Result<AparienciaDetalleViewModel>>;

public record EliminarLogoCommand(Guid Id) : IRequest<Result<AparienciaDetalleViewModel>>;

#endregion

#region Validators

public static partial class ColorHex
{
    [GeneratedRegex("^#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")]
    private static partial Regex Patron();

    public static bool EsValido(string? valor) => valor is not null && Patron().IsMatch(valor.Trim());

    /// <summary>Convierte #abc, abc o #aabbcc al formato #AABBCC.</summary>
    public static string Normalizar(string valor)
    {
        var hex = valor.Trim().TrimStart('#').ToUpperInvariant();
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }

        return "#" + hex;
    }
}

public class DatosAparienciaValidator<T> : AbstractValidator<T> where T : IDatosApariencia
{
    public DatosAparienciaValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(300);
        RuleFor(x => x.NombreSistema).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NombreCorto).MaximumLength(30);
        RuleFor(x => x.NombreInstitucion).MaximumLength(200);

        RuleFor(x => x.ColorPrimario).Must(ColorHex.EsValido).WithMessage(Mensaje("primario"));
        RuleFor(x => x.ColorSecundario).Must(ColorHex.EsValido).WithMessage(Mensaje("secundario"));
        RuleFor(x => x.ColorBotonPrimario).Must(ColorHex.EsValido).WithMessage(Mensaje("del botón primario"));
        RuleFor(x => x.ColorBotonSecundario).Must(ColorHex.EsValido).WithMessage(Mensaje("del botón secundario"));
        RuleFor(x => x.ColorBotonPeligro).Must(ColorHex.EsValido).WithMessage(Mensaje("del botón de peligro"));
    }

    private static string Mensaje(string color) => $"El color {color} debe estar en formato HEX (#RRGGBB).";
}

public class CreateAparienciaCommandValidator : DatosAparienciaValidator<CreateAparienciaCommand>;

public class UpdateAparienciaCommandValidator : DatosAparienciaValidator<UpdateAparienciaCommand>
{
    public UpdateAparienciaCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class DuplicarAparienciaCommandValidator : AbstractValidator<DuplicarAparienciaCommand>
{
    public DuplicarAparienciaCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class ActualizarLogoCommandValidator : AbstractValidator<ActualizarLogoCommand>
{
    public const int TamanoMaximoBytes = 1024 * 1024;

    public ActualizarLogoCommandValidator()
    {
        RuleFor(x => x.NombreArchivo).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileBase64)
            .NotEmpty()
            .Must(b => Imagen.Decodificar(b) is not null).WithMessage("El logo no tiene un formato Base64 válido.")
            .Must(b => (Imagen.Decodificar(b)?.Length ?? 0) <= TamanoMaximoBytes).WithMessage("El logo no puede superar 1 MB.");
    }
}

#endregion

#region Handlers

public class AparienciaQueryHandler(IUnitOfWork uow) :
    IRequestHandler<GetAparienciaQuery, Result<AparienciaViewModel>>,
    IRequestHandler<GetLogoQuery, Result<LogoArchivo>>,
    IRequestHandler<GetAparienciasQuery, Result<IReadOnlyList<AparienciaDetalleViewModel>>>,
    IRequestHandler<GetAparienciaByIdQuery, Result<AparienciaDetalleViewModel>>,
    IRequestHandler<GetLogoAparienciaQuery, Result<LogoArchivo>>
{
    public async Task<Result<AparienciaViewModel>> Handle(GetAparienciaQuery request, CancellationToken cancellationToken)
    {
        var activa = await AparienciaStore.Detalles(uow).FirstOrDefaultAsync(a => a.EsActiva, cancellationToken)
            ?? AparienciaStore.ToDetalle(Apariencia.Predeterminada());

        return Result.Success(new AparienciaViewModel
        {
            Id = activa.Id,
            NombreSistema = activa.NombreSistema,
            NombreCorto = activa.NombreCorto,
            NombreInstitucion = activa.NombreInstitucion,
            TieneLogo = activa.TieneLogo,
            ColorPrimario = activa.ColorPrimario,
            ColorSecundario = activa.ColorSecundario,
            ColorBotonPrimario = activa.ColorBotonPrimario,
            ColorBotonSecundario = activa.ColorBotonSecundario,
            ColorBotonPeligro = activa.ColorBotonPeligro,
            Version = activa.Version,
            ActualizadoEn = activa.ActualizadoEn
        });
    }

    public Task<Result<LogoArchivo>> Handle(GetLogoQuery request, CancellationToken cancellationToken) =>
        LogoAsync(uow.Query<Apariencia>().Where(a => a.EsActiva), cancellationToken);

    public async Task<Result<IReadOnlyList<AparienciaDetalleViewModel>>> Handle(GetAparienciasQuery request, CancellationToken cancellationToken)
    {
        var apariencias = await AparienciaStore.Detalles(uow)
            .OrderByDescending(a => a.EsActiva).ThenBy(a => a.Nombre)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AparienciaDetalleViewModel>>(apariencias);
    }

    public Task<Result<AparienciaDetalleViewModel>> Handle(GetAparienciaByIdQuery request, CancellationToken cancellationToken) =>
        AparienciaStore.DetalleAsync(uow, request.Id, cancellationToken);

    public Task<Result<LogoArchivo>> Handle(GetLogoAparienciaQuery request, CancellationToken cancellationToken) =>
        LogoAsync(uow.Query<Apariencia>().Where(a => a.Id == request.Id), cancellationToken);

    private static async Task<Result<LogoArchivo>> LogoAsync(IQueryable<Apariencia> apariencia, CancellationToken cancellationToken)
    {
        var logo = await apariencia
            .Where(a => a.Logo != null)
            .Select(a => new LogoArchivo(a.Logo!, a.LogoContentType!, a.Id, a.Version))
            .FirstOrDefaultAsync(cancellationToken);

        return logo is null ? Result.Failure<LogoArchivo>(ConfiguracionErrors.LogoNoConfigurado) : Result.Success(logo);
    }
}

public class AparienciaCommandHandler(IUnitOfWork uow) :
    IRequestHandler<CreateAparienciaCommand, Result<Guid>>,
    IRequestHandler<UpdateAparienciaCommand, Result<AparienciaDetalleViewModel>>,
    IRequestHandler<DuplicarAparienciaCommand, Result<Guid>>,
    IRequestHandler<ActivarAparienciaCommand, Result>,
    IRequestHandler<ActualizarLogoCommand, Result<AparienciaDetalleViewModel>>,
    IRequestHandler<EliminarLogoCommand, Result<AparienciaDetalleViewModel>>
{
    public async Task<Result<Guid>> Handle(CreateAparienciaCommand request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre.Trim();
        if (await NombreEnUsoAsync(nombre, null, cancellationToken))
        {
            return Result.Failure<Guid>(ConfiguracionErrors.NombreDuplicado(nombre));
        }

        // Si la base no tiene ninguna apariencia, la primera queda activa.
        var hayActiva = await uow.Query<Apariencia>().AnyAsync(a => a.EsActiva, cancellationToken);

        var apariencia = new Apariencia
        {
            Nombre = nombre,
            NombreSistema = request.NombreSistema.Trim(),
            ColorPrimario = ColorHex.Normalizar(request.ColorPrimario),
            ColorSecundario = ColorHex.Normalizar(request.ColorSecundario),
            ColorBotonPrimario = ColorHex.Normalizar(request.ColorBotonPrimario),
            ColorBotonSecundario = ColorHex.Normalizar(request.ColorBotonSecundario),
            ColorBotonPeligro = ColorHex.Normalizar(request.ColorBotonPeligro),
            EsActiva = !hayActiva,
            Status = true,
            Version = 1
        };
        Aplicar(apariencia, request);

        await uow.AddAsync(apariencia, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(apariencia.Id);
    }

    public async Task<Result<AparienciaDetalleViewModel>> Handle(UpdateAparienciaCommand request, CancellationToken cancellationToken)
    {
        var apariencia = await uow.Tracked<Apariencia>().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (apariencia is null)
        {
            return Result.Failure<AparienciaDetalleViewModel>(ConfiguracionErrors.AparienciaNotFound(request.Id));
        }

        var nombre = request.Nombre.Trim();
        if (await NombreEnUsoAsync(nombre, request.Id, cancellationToken))
        {
            return Result.Failure<AparienciaDetalleViewModel>(ConfiguracionErrors.NombreDuplicado(nombre));
        }

        apariencia.Nombre = nombre;
        Aplicar(apariencia, request);
        apariencia.Version++;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(AparienciaStore.ToDetalle(apariencia));
    }

    public async Task<Result<Guid>> Handle(DuplicarAparienciaCommand request, CancellationToken cancellationToken)
    {
        var origen = await uow.Query<Apariencia>().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (origen is null) return Result.Failure<Guid>(ConfiguracionErrors.AparienciaNotFound(request.Id));

        var nombre = request.Nombre.Trim();
        if (await NombreEnUsoAsync(nombre, null, cancellationToken))
        {
            return Result.Failure<Guid>(ConfiguracionErrors.NombreDuplicado(nombre));
        }

        var copia = new Apariencia
        {
            Nombre = nombre,
            Descripcion = origen.Descripcion,
            EsActiva = false,
            NombreSistema = origen.NombreSistema,
            NombreCorto = origen.NombreCorto,
            NombreInstitucion = origen.NombreInstitucion,
            Logo = origen.Logo,
            LogoContentType = origen.LogoContentType,
            LogoNombreArchivo = origen.LogoNombreArchivo,
            ColorPrimario = origen.ColorPrimario,
            ColorSecundario = origen.ColorSecundario,
            ColorBotonPrimario = origen.ColorBotonPrimario,
            ColorBotonSecundario = origen.ColorBotonSecundario,
            ColorBotonPeligro = origen.ColorBotonPeligro,
            Status = true,
            Version = 1
        };

        await uow.AddAsync(copia, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(copia.Id);
    }

    public async Task<Result> Handle(ActivarAparienciaCommand request, CancellationToken cancellationToken)
    {
        var apariencia = await uow.Tracked<Apariencia>().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (apariencia is null) return Result.Failure(ConfiguracionErrors.AparienciaNotFound(request.Id));
        if (apariencia.EsActiva) return Result.Success();

        // Dos guardados: el índice único filtrado no admite dos activas ni por un instante.
        return await uow.EnTransaccionAsync(async () =>
        {
            var anteriores = await uow.Tracked<Apariencia>().Where(a => a.EsActiva).ToListAsync(cancellationToken);
            anteriores.ForEach(a => a.EsActiva = false);
            await uow.SaveChangesAsync(cancellationToken);

            apariencia.EsActiva = true;
            await uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }

    public async Task<Result<AparienciaDetalleViewModel>> Handle(ActualizarLogoCommand request, CancellationToken cancellationToken)
    {
        var contenido = Imagen.Decodificar(request.FileBase64)!;
        var contentType = Imagen.DetectarContentType(contenido);
        if (contentType is null)
        {
            return Result.Failure<AparienciaDetalleViewModel>(ConfiguracionErrors.FormatoLogoInvalido);
        }

        var apariencia = await uow.Tracked<Apariencia>().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (apariencia is null)
        {
            return Result.Failure<AparienciaDetalleViewModel>(ConfiguracionErrors.AparienciaNotFound(request.Id));
        }

        apariencia.CambiarLogo(contenido, contentType, Path.GetFileName(request.NombreArchivo.Trim()));
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(AparienciaStore.ToDetalle(apariencia));
    }

    public async Task<Result<AparienciaDetalleViewModel>> Handle(EliminarLogoCommand request, CancellationToken cancellationToken)
    {
        var apariencia = await uow.Tracked<Apariencia>().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (apariencia is null)
        {
            return Result.Failure<AparienciaDetalleViewModel>(ConfiguracionErrors.AparienciaNotFound(request.Id));
        }

        if (apariencia.Logo is not null)
        {
            apariencia.CambiarLogo(null, null, null);
            await uow.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(AparienciaStore.ToDetalle(apariencia));
    }

    private Task<bool> NombreEnUsoAsync(string nombre, Guid? excluirId, CancellationToken cancellationToken) =>
        uow.Query<Apariencia>().AnyAsync(a => a.Nombre == nombre && a.Id != excluirId, cancellationToken);

    private static void Aplicar(Apariencia apariencia, IDatosApariencia datos)
    {
        apariencia.Descripcion = Limpiar(datos.Descripcion);
        apariencia.NombreSistema = datos.NombreSistema.Trim();
        apariencia.NombreCorto = Limpiar(datos.NombreCorto);
        apariencia.NombreInstitucion = Limpiar(datos.NombreInstitucion);
        apariencia.ColorPrimario = ColorHex.Normalizar(datos.ColorPrimario);
        apariencia.ColorSecundario = ColorHex.Normalizar(datos.ColorSecundario);
        apariencia.ColorBotonPrimario = ColorHex.Normalizar(datos.ColorBotonPrimario);
        apariencia.ColorBotonSecundario = ColorHex.Normalizar(datos.ColorBotonSecundario);
        apariencia.ColorBotonPeligro = ColorHex.Normalizar(datos.ColorBotonPeligro);
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

internal static class AparienciaStore
{
    /// <summary>Proyección sin el contenido del logo.</summary>
    public static IQueryable<AparienciaDetalleViewModel> Detalles(IUnitOfWork uow) =>
        uow.Query<Apariencia>().Select(a => new AparienciaDetalleViewModel
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Descripcion = a.Descripcion,
            EsActiva = a.EsActiva,
            NombreSistema = a.NombreSistema,
            NombreCorto = a.NombreCorto,
            NombreInstitucion = a.NombreInstitucion,
            TieneLogo = a.Logo != null,
            LogoNombreArchivo = a.LogoNombreArchivo,
            ColorPrimario = a.ColorPrimario,
            ColorSecundario = a.ColorSecundario,
            ColorBotonPrimario = a.ColorBotonPrimario,
            ColorBotonSecundario = a.ColorBotonSecundario,
            ColorBotonPeligro = a.ColorBotonPeligro,
            Version = a.Version,
            ActualizadoEn = a.UpdatedAt
        });

    public static async Task<Result<AparienciaDetalleViewModel>> DetalleAsync(IUnitOfWork uow, Guid id, CancellationToken cancellationToken)
    {
        var apariencia = await Detalles(uow).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return apariencia is null
            ? Result.Failure<AparienciaDetalleViewModel>(ConfiguracionErrors.AparienciaNotFound(id))
            : Result.Success(apariencia);
    }

    public static AparienciaDetalleViewModel ToDetalle(Apariencia a) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Descripcion = a.Descripcion,
        EsActiva = a.EsActiva,
        NombreSistema = a.NombreSistema,
        NombreCorto = a.NombreCorto,
        NombreInstitucion = a.NombreInstitucion,
        TieneLogo = a.Logo is not null,
        LogoNombreArchivo = a.LogoNombreArchivo,
        ColorPrimario = a.ColorPrimario,
        ColorSecundario = a.ColorSecundario,
        ColorBotonPrimario = a.ColorBotonPrimario,
        ColorBotonSecundario = a.ColorBotonSecundario,
        ColorBotonPeligro = a.ColorBotonPeligro,
        Version = a.Version,
        ActualizadoEn = a.UpdatedAt
    };
}

#endregion
