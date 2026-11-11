using Application.Contracts;
using Application.Features.Configuracion;
using Domain.Entities.Configuracion;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reportes;

public static class EncabezadoPdf
{
    /// <summary>
    /// Encabezado de un documento PDF: la plantilla predeterminada del tipo; sin ella, el título
    /// predeterminado. Logo: el de la plantilla; si no tiene, el de la apariencia activa; si tampoco,
    /// el generador usa el predeterminado del sistema.
    /// </summary>
    public static async Task<DatosReportePdf> ObtenerAsync(
        IMediator mediator, IUnitOfWork uow, TipoReportePdf tipo, CancellationToken cancellationToken)
    {
        var plantilla = await mediator.Send(new GetDatosReportePdfQuery(tipo), cancellationToken);
        var encabezado = plantilla.IsSuccess
            ? plantilla.Value
            : new DatosReportePdf
            {
                Plantilla = "Predeterminada",
                TipoReporte = tipo,
                Titulo = TiposReportePdf.TituloPredeterminado(tipo)
            };

        if (encabezado.Logos.Count == 0)
        {
            var logo = await uow.Query<Apariencia>()
                .Where(x => x.EsActiva && x.Logo != null)
                .Select(x => new { x.Logo, x.LogoContentType })
                .FirstOrDefaultAsync(cancellationToken);

            if (logo?.Logo is { Length: > 0 } contenido)
            {
                encabezado.Logos.Add(new LogoReporte
                {
                    Posicion = PosicionLogo.Izquierda,
                    ContentType = logo.LogoContentType ?? "image/png",
                    Contenido = contenido
                });
            }
        }

        return encabezado;
    }
}
