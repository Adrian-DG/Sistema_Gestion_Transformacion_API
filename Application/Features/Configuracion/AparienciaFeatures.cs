using Application.Contracts;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Configuracion;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Application.Features.Configuracion;

public class AparienciaViewModel
{
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

public record LogoArchivo(byte[] Contenido, string ContentType, int Version);

public record GetAparienciaQuery : IRequest<Result<AparienciaViewModel>>;

public record GetLogoQuery : IRequest<Result<LogoArchivo>>;

public record UpdateAparienciaCommand(
    string NombreSistema,
    string? NombreCorto,
    string? NombreInstitucion,
    string ColorPrimario,
    string ColorSecundario,
    string ColorBotonPrimario,
    string ColorBotonSecundario,
    string ColorBotonPeligro
) : IRequest<Result<AparienciaViewModel>>;

/// <param name="FileBase64">Imagen PNG, JPEG o WebP. El tipo se determina por el contenido, no por el nombre.</param>
public record ActualizarLogoCommand(string NombreArchivo, string FileBase64) : IRequest<Result<AparienciaViewModel>>;

public record EliminarLogoCommand : IRequest<Result<AparienciaViewModel>>;

/// <summary>Vuelve textos y colores a los valores institucionales; el logo se conserva.</summary>
public record RestablecerAparienciaCommand : IRequest<Result<AparienciaViewModel>>;

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

public class UpdateAparienciaCommandValidator : AbstractValidator<UpdateAparienciaCommand>
{
    public UpdateAparienciaCommandValidator()
    {
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

public class ActualizarLogoCommandValidator : AbstractValidator<ActualizarLogoCommand>
{
    public const int TamanoMaximoBytes = 1024 * 1024;

    public ActualizarLogoCommandValidator()
    {
        RuleFor(x => x.NombreArchivo).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileBase64)
            .NotEmpty()
            .Must(b => Logo.Decodificar(b) is not null).WithMessage("El logo no tiene un formato Base64 válido.")
            .Must(b => (Logo.Decodificar(b)?.Length ?? 0) <= TamanoMaximoBytes).WithMessage("El logo no puede superar 1 MB.");
    }
}

internal static class Logo
{
    /// <summary>Acepta Base64 puro o un data URL ("data:image/png;base64,...").</summary>
    public static byte[]? Decodificar(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;

        var coma = base64.IndexOf(',');
        if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && coma > 0)
        {
            base64 = base64[(coma + 1)..];
        }

        var buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64.Trim(), buffer, out var escritos) ? buffer[..escritos] : null;
    }

    /// <summary>
    /// Identifica el formato por su firma. SVG queda excluido a propósito: puede contener scripts
    /// y el logo se sirve sin autenticación.
    /// </summary>
    public static string? DetectarContentType(byte[] contenido) => contenido switch
    {
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => "image/png",
        [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => "image/webp",
        _ => null
    };
}

public class AparienciaQueryHandler(IUnitOfWork uow) :
    IRequestHandler<GetAparienciaQuery, Result<AparienciaViewModel>>,
    IRequestHandler<GetLogoQuery, Result<LogoArchivo>>
{
    public async Task<Result<AparienciaViewModel>> Handle(GetAparienciaQuery request, CancellationToken cancellationToken)
    {
        var apariencia = await AparienciaStore.ProyectarAsync(uow, cancellationToken);
        return Result.Success(apariencia ?? AparienciaStore.ToViewModel(Apariencia.Predeterminada()));
    }

    public async Task<Result<LogoArchivo>> Handle(GetLogoQuery request, CancellationToken cancellationToken)
    {
        var logo = await uow.Query<Apariencia>()
            .Where(a => a.Id == Apariencia.IdUnico && a.Logo != null)
            .Select(a => new LogoArchivo(a.Logo!, a.LogoContentType!, a.Version))
            .FirstOrDefaultAsync(cancellationToken);

        return logo is null ? Result.Failure<LogoArchivo>(ConfiguracionErrors.LogoNoConfigurado) : Result.Success(logo);
    }
}

public class AparienciaCommandHandler(IUnitOfWork uow) :
    IRequestHandler<UpdateAparienciaCommand, Result<AparienciaViewModel>>,
    IRequestHandler<ActualizarLogoCommand, Result<AparienciaViewModel>>,
    IRequestHandler<EliminarLogoCommand, Result<AparienciaViewModel>>,
    IRequestHandler<RestablecerAparienciaCommand, Result<AparienciaViewModel>>
{
    public async Task<Result<AparienciaViewModel>> Handle(UpdateAparienciaCommand request, CancellationToken cancellationToken)
    {
        var apariencia = await AparienciaStore.ObtenerParaEditarAsync(uow, cancellationToken);

        apariencia.NombreSistema = request.NombreSistema.Trim();
        apariencia.NombreCorto = Limpiar(request.NombreCorto);
        apariencia.NombreInstitucion = Limpiar(request.NombreInstitucion);
        apariencia.ColorPrimario = ColorHex.Normalizar(request.ColorPrimario);
        apariencia.ColorSecundario = ColorHex.Normalizar(request.ColorSecundario);
        apariencia.ColorBotonPrimario = ColorHex.Normalizar(request.ColorBotonPrimario);
        apariencia.ColorBotonSecundario = ColorHex.Normalizar(request.ColorBotonSecundario);
        apariencia.ColorBotonPeligro = ColorHex.Normalizar(request.ColorBotonPeligro);
        apariencia.Version++;

        return await GuardarAsync(apariencia, cancellationToken);
    }

    public async Task<Result<AparienciaViewModel>> Handle(ActualizarLogoCommand request, CancellationToken cancellationToken)
    {
        var contenido = Logo.Decodificar(request.FileBase64)!;
        var contentType = Logo.DetectarContentType(contenido);
        if (contentType is null)
        {
            return Result.Failure<AparienciaViewModel>(ConfiguracionErrors.FormatoLogoInvalido);
        }

        var apariencia = await AparienciaStore.ObtenerParaEditarAsync(uow, cancellationToken);
        apariencia.CambiarLogo(contenido, contentType, Path.GetFileName(request.NombreArchivo.Trim()));

        return await GuardarAsync(apariencia, cancellationToken);
    }

    public async Task<Result<AparienciaViewModel>> Handle(EliminarLogoCommand request, CancellationToken cancellationToken)
    {
        var apariencia = await AparienciaStore.ObtenerParaEditarAsync(uow, cancellationToken);
        if (apariencia.Logo is null)
        {
            return Result.Success(AparienciaStore.ToViewModel(apariencia));
        }

        apariencia.CambiarLogo(null, null, null);
        return await GuardarAsync(apariencia, cancellationToken);
    }

    public async Task<Result<AparienciaViewModel>> Handle(RestablecerAparienciaCommand request, CancellationToken cancellationToken)
    {
        var apariencia = await AparienciaStore.ObtenerParaEditarAsync(uow, cancellationToken);
        apariencia.Restablecer();

        return await GuardarAsync(apariencia, cancellationToken);
    }

    private async Task<Result<AparienciaViewModel>> GuardarAsync(Apariencia apariencia, CancellationToken cancellationToken)
    {
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(AparienciaStore.ToViewModel(apariencia));
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

internal static class AparienciaStore
{
    /// <summary>Lee la configuración sin cargar el contenido del logo.</summary>
    public static Task<AparienciaViewModel?> ProyectarAsync(IUnitOfWork uow, CancellationToken cancellationToken) =>
        uow.Query<Apariencia>()
            .Where(a => a.Id == Apariencia.IdUnico)
            .Select(a => new AparienciaViewModel
            {
                NombreSistema = a.NombreSistema,
                NombreCorto = a.NombreCorto,
                NombreInstitucion = a.NombreInstitucion,
                TieneLogo = a.Logo != null,
                ColorPrimario = a.ColorPrimario,
                ColorSecundario = a.ColorSecundario,
                ColorBotonPrimario = a.ColorBotonPrimario,
                ColorBotonSecundario = a.ColorBotonSecundario,
                ColorBotonPeligro = a.ColorBotonPeligro,
                Version = a.Version,
                ActualizadoEn = a.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Devuelve el registro con seguimiento; si no existe (base sin datos iniciales) lo crea.</summary>
    public static async Task<Apariencia> ObtenerParaEditarAsync(IUnitOfWork uow, CancellationToken cancellationToken)
    {
        var apariencia = await uow.Tracked<Apariencia>().FirstOrDefaultAsync(a => a.Id == Apariencia.IdUnico, cancellationToken);
        if (apariencia is not null) return apariencia;

        apariencia = Apariencia.Predeterminada();
        await uow.AddAsync(apariencia, cancellationToken);
        return apariencia;
    }

    public static AparienciaViewModel ToViewModel(Apariencia a) => new()
    {
        NombreSistema = a.NombreSistema,
        NombreCorto = a.NombreCorto,
        NombreInstitucion = a.NombreInstitucion,
        TieneLogo = a.Logo is not null,
        ColorPrimario = a.ColorPrimario,
        ColorSecundario = a.ColorSecundario,
        ColorBotonPrimario = a.ColorBotonPrimario,
        ColorBotonSecundario = a.ColorBotonSecundario,
        ColorBotonPeligro = a.ColorBotonPeligro,
        Version = a.Version,
        ActualizadoEn = a.UpdatedAt
    };
}
