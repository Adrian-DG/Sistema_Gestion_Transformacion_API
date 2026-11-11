using Application.Contracts.Reportes;
using Application.Features.Configuracion;
using Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Infraestructure.Reportes
{
    /// <summary>
    /// Reportes PDF con QuestPDF. El encabezado sale de la plantilla de reportes (logos, texto, título,
    /// nota al pie y firmante); sin logos se usa el logo predeterminado del sistema.
    /// </summary>
    public class QuestPdfReportGenerator : IPdfReportGenerator
    {
        private const string ColorPrimario = "#0B2A4A";
        private const string ColorTexto = "#1E293B";
        private const string ColorSecundario = "#64748B";
        private const string ColorBorde = "#CBD5E1";
        private const string ColorFondo = "#F1F5F9";

        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");
        private static readonly Lazy<string> LogoPredeterminado = new(LeerLogoPredeterminado);

        public byte[] ConstanciaAsignacion(DatosReportePdf encabezado, ConstanciaAsignacion d)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.Letter);
                    pagina.Margin(36);
                    pagina.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(ColorTexto));

                    pagina.Header().Element(c => Encabezado(c, encabezado, d));
                    pagina.Content().PaddingVertical(10).Element(c => Contenido(c, d, encabezado.Firmante));
                    pagina.Footer().Element(c => Pie(c, encabezado.NotaPiePagina, d.Generado));
                });
            }).GeneratePdf();
        }

        // ---------------- Tickets de combustible ----------------

        /// <summary>Tickets por página: 2 columnas por 4 filas, con línea de corte punteada.</summary>
        private const int ColumnasTickets = 2;

        public byte[] TicketsCombustible(DatosReportePdf encabezado, TicketsImpresion d)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.Letter);
                    pagina.Margin(24);
                    pagina.DefaultTextStyle(t => t.FontSize(8.5f).FontColor(ColorTexto));

                    pagina.Content().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            for (var i = 0; i < ColumnasTickets; i++) c.RelativeColumn();
                        });

                        foreach (var ticket in d.Tickets)
                        {
                            tabla.Cell().Padding(4).Element(c => Ticket(c, encabezado, d, ticket));
                        }
                    });

                    pagina.Footer().Element(c => Pie(c, encabezado.NotaPiePagina, d.Generado));
                });
            }).GeneratePdf();
        }

        private static void Ticket(IContainer contenedor, DatosReportePdf e, TicketsImpresion d, TicketImpresion t)
        {
            contenedor
                .Height(168)
                // Línea de corte.
                .Border(0.75f).BorderColor(ColorSecundario)
                .Padding(8)
                .Column(columna =>
                {
                    columna.Spacing(3);

                    columna.Item().Row(fila =>
                    {
                        fila.ConstantItem(34).Height(28).Element(c => Logo(c, e, PosicionLogo.Izquierda, predeterminado: true));
                        fila.RelativeItem().PaddingLeft(6).Column(texto =>
                        {
                            if (e.LineasEncabezado.FirstOrDefault() is { } institucion)
                            {
                                texto.Item().Text(institucion).SemiBold().FontSize(7.5f).FontColor(ColorSecundario);
                            }
                            texto.Item().Text(e.Titulo.ToUpper(Cultura)).Bold().FontSize(10).FontColor(ColorPrimario);
                        });
                        fila.AutoItem().AlignRight().Column(texto =>
                        {
                            texto.Item().AlignRight().Text(t.TipoCombustible.ToUpper(Cultura)).Bold().FontSize(11);
                            texto.Item().AlignRight().Text($"{t.Galones.ToString("0.##", Cultura)} galones").SemiBold();
                        });
                    });

                    columna.Item().LineHorizontal(0.75f).LineColor(ColorPrimario);

                    columna.Item().Text(texto =>
                    {
                        texto.Span("Entregado a: ").FontColor(ColorSecundario);
                        texto.Span(d.EntregadoACedula is null ? d.EntregadoA : $"{d.EntregadoA} ({d.EntregadoACedula})").SemiBold();
                    });
                    columna.Item().Text(texto =>
                    {
                        texto.Span("Propósito: ").FontColor(ColorSecundario);
                        texto.Span(d.Proposito);
                    });

                    columna.Item().PaddingTop(4).Height(42).Svg(Code128.Svg(t.CodigoBarras)).FitArea();
                    columna.Item().Row(fila =>
                    {
                        fila.RelativeItem().Text(t.Numero).Bold().FontSize(10).LetterSpacing(0.05f);
                        if (t.CodigoVerificacion is not null)
                        {
                            fila.AutoItem().AlignRight().Text(texto =>
                            {
                                texto.Span("Verificación ").FontSize(7.5f).FontColor(ColorSecundario);
                                texto.Span(t.CodigoVerificacion).Bold().FontSize(10).LetterSpacing(0.05f);
                            });
                        }
                    });

                    columna.Item().Text(texto =>
                    {
                        texto.DefaultTextStyle(s => s.FontSize(7).FontColor(ColorSecundario));
                        texto.Span("Válido por un solo despacho. ");
                        if (d.FechaAprobacion is { } aprobado)
                        {
                            texto.Span($"Aprobado el {aprobado.ToString("dd/MM/yyyy", Cultura)}.");
                        }
                    });
                });
        }

        // ---------------- Encabezado ----------------

        private static void Encabezado(IContainer contenedor, DatosReportePdf e, ConstanciaAsignacion d)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Row(fila =>
                {
                    fila.ConstantItem(80).Height(60).Element(c => Logo(c, e, PosicionLogo.Izquierda, predeterminado: true));

                    fila.RelativeItem().AlignCenter().Column(texto =>
                    {
                        // Un logo central se coloca sobre el texto del encabezado.
                        if (e.Logos.Any(l => l.Posicion == PosicionLogo.Centro))
                        {
                            texto.Item().AlignCenter().Height(40).Element(c => Logo(c, e, PosicionLogo.Centro, predeterminado: false));
                        }

                        foreach (var linea in e.LineasEncabezado)
                        {
                            texto.Item().AlignCenter().Text(linea).SemiBold().FontSize(10);
                        }
                    });

                    fila.ConstantItem(80).Height(60).Element(c => Logo(c, e, PosicionLogo.Derecha, predeterminado: false));
                });

                columna.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColorPrimario);

                columna.Item().PaddingTop(8).Row(fila =>
                {
                    fila.RelativeItem().Column(t =>
                    {
                        t.Item().Text(e.Titulo).Bold().FontSize(14).FontColor(ColorPrimario);
                        if (!string.IsNullOrWhiteSpace(e.Subtitulo))
                        {
                            t.Item().Text(e.Subtitulo).FontColor(ColorSecundario);
                        }
                    });

                    fila.AutoItem().AlignRight().Column(t =>
                    {
                        t.Item().AlignRight().Text(text =>
                        {
                            text.Span("Ref.: ").FontColor(ColorSecundario);
                            text.Span(d.Referencia).SemiBold();
                        });
                        t.Item().AlignRight().PaddingTop(2)
                            .Background(d.EsValida ? "#DCFCE7" : "#FEF3C7")
                            .PaddingHorizontal(6).PaddingVertical(2)
                            .Text(d.Estatus).SemiBold().FontSize(8.5f)
                            .FontColor(d.EsValida ? "#166534" : "#92400E");
                    });
                });
            });
        }

        /// <summary>Logo de la posición indicada; sin logos en la plantilla, el predeterminado va a la izquierda.</summary>
        private static void Logo(IContainer contenedor, DatosReportePdf e, PosicionLogo posicion, bool predeterminado)
        {
            var logo = e.Logos.FirstOrDefault(l => l.Posicion == posicion);
            if (logo is not null)
            {
                if (logo.ContentType.Contains("svg", StringComparison.OrdinalIgnoreCase))
                {
                    contenedor.AlignMiddle().Svg(System.Text.Encoding.UTF8.GetString(logo.Contenido)).FitArea();
                }
                else
                {
                    contenedor.AlignMiddle().Image(logo.Contenido).FitArea();
                }
                return;
            }

            if (predeterminado && e.Logos.Count == 0)
            {
                contenedor.AlignMiddle().Svg(LogoPredeterminado.Value).FitArea();
            }
        }

        // ---------------- Contenido ----------------

        private static void Contenido(IContainer contenedor, ConstanciaAsignacion d, FirmanteReporte? firmante)
        {
            contenedor.Column(columna =>
            {
                columna.Spacing(10);

                if (!d.EsValida)
                {
                    columna.Item().Background("#FEF3C7").Border(0.75f).BorderColor("#F59E0B").Padding(6)
                        .Text($"Este documento no constituye una asignación vigente: la solicitud está {d.Estatus.ToLower(Cultura)}.")
                        .FontColor("#92400E").SemiBold();
                }

                columna.Item().Element(c => Seccion(c, "Datos de la solicitud", tabla =>
                {
                    Par(tabla, "Fecha de solicitud", FechaHora(d.FechaSolicitud), "Registrada por", d.RegistradoPor);
                    Par(tabla, "Fecha de aprobación", d.FechaAprobacion is { } f ? FechaHora(f) : "—", "Aprobada por", d.AprobadoPor ?? "—");
                }));

                columna.Item().Element(c => Seccion(c, "Vehículo", tabla =>
                {
                    var v = d.Vehiculo;
                    Par(tabla, "Placa", v.Placa, "Ficha", v.Ficha ?? "—");
                    Par(tabla, "Marca y modelo", $"{v.Marca} {v.Modelo} ({v.Fabricacion})", "Tipo y color", $"{v.Tipo}, {v.Color}");
                    Par(tabla, "Chasis", v.Chasis, "Matrícula", v.Matricula);
                    Fila(tabla, "Kilometraje registrado", $"{v.Kilometraje.ToString("N0", Cultura)} km");
                }));

                columna.Item().Element(c => Seccion(c, "Solicitante", tabla =>
                {
                    Par(tabla, "Nombre", Persona(d.Solicitante), "Cédula", d.Solicitante.Cedula);
                    Par(tabla, "Cargo", d.CargoSolicitante, "Dependencia", d.DependenciaSolicitante);
                }));

                columna.Item().Element(c => Seccion(c, "Asignado a", tabla =>
                {
                    Par(tabla, "Nombre", Persona(d.Asignado), "Cédula", d.Asignado.Cedula);
                    Par(tabla, "Tipo de asignación", d.CargoAsignado is null ? "A título personal" : "Al cargo",
                        "Cargo", d.CargoAsignado ?? "—");
                    Par(tabla, "Chofer", Persona(d.Chofer), "Cédula del chofer", d.Chofer.Cedula);
                }));

                columna.Item().Element(c => Seccion(c, "Período y destino", tabla =>
                {
                    Par(tabla, "Salida", FechaHora(d.FechaSalida), "Retorno estimado", FechaHora(d.FechaRetornoEstimada));
                    Par(tabla, "Retorno real", d.FechaRetornoReal is { } r ? FechaHora(r) : "—", "Destino", d.Destino ?? "—");
                    Fila(tabla, "Propósito", d.Proposito);
                    if (!string.IsNullOrWhiteSpace(d.MotivoRechazoCancelacion))
                    {
                        Fila(tabla, "Motivo de rechazo o cancelación", d.MotivoRechazoCancelacion);
                    }
                }));

                columna.Item().PaddingTop(30).Element(c => Firmas(c, d, firmante));
            });
        }

        private static void Seccion(IContainer contenedor, string titulo, Action<TableDescriptor> filas)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Background(ColorPrimario).PaddingHorizontal(6).PaddingVertical(3)
                    .Text(titulo.ToUpper(Cultura)).Bold().FontSize(8.5f).FontColor(Colors.White);

                columna.Item().Border(0.5f).BorderColor(ColorBorde).Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(105);
                        c.RelativeColumn();
                        c.ConstantColumn(105);
                        c.RelativeColumn();
                    });
                    filas(tabla);
                });
            });
        }

        private static void Par(TableDescriptor tabla, string etiqueta1, string valor1, string etiqueta2, string valor2)
        {
            Celda(tabla.Cell(), etiqueta1, esEtiqueta: true);
            Celda(tabla.Cell(), valor1, esEtiqueta: false);
            Celda(tabla.Cell(), etiqueta2, esEtiqueta: true);
            Celda(tabla.Cell(), valor2, esEtiqueta: false);
        }

        /// <summary>Etiqueta y un valor que ocupa el resto de la fila.</summary>
        private static void Fila(TableDescriptor tabla, string etiqueta, string valor)
        {
            Celda(tabla.Cell(), etiqueta, esEtiqueta: true);
            Celda(tabla.Cell().ColumnSpan(3), valor, esEtiqueta: false);
        }

        private static void Celda(IContainer celda, string texto, bool esEtiqueta)
        {
            var c = celda.BorderBottom(0.5f).BorderColor(ColorBorde).PaddingHorizontal(6).PaddingVertical(4);
            if (esEtiqueta)
            {
                c.Background(ColorFondo).Text(texto).FontSize(8.5f).FontColor(ColorSecundario);
            }
            else
            {
                c.Text(texto);
            }
        }

        private static void Firmas(IContainer contenedor, ConstanciaAsignacion d, FirmanteReporte? firmante)
        {
            contenedor.Row(fila =>
            {
                fila.Spacing(24);
                fila.RelativeItem().Element(c => Firma(c, "Entregado por",
                    firmante?.NombreCompleto, firmante?.Cargo));
                fila.RelativeItem().Element(c => Firma(c, "Recibido por",
                    Persona(d.Asignado), d.CargoAsignado ?? $"Cédula {d.Asignado.Cedula}"));
                fila.RelativeItem().Element(c => Firma(c, "Chofer",
                    Persona(d.Chofer), $"Cédula {d.Chofer.Cedula}"));
            });
        }

        private static void Firma(IContainer contenedor, string rol, string? nombre, string? detalle)
        {
            contenedor.Column(c =>
            {
                c.Item().PaddingTop(28).LineHorizontal(0.75f).LineColor(ColorTexto);
                c.Item().AlignCenter().Text(nombre ?? " ").SemiBold().FontSize(8.5f);
                if (!string.IsNullOrWhiteSpace(detalle))
                {
                    c.Item().AlignCenter().Text(detalle).FontSize(8).FontColor(ColorSecundario);
                }
                c.Item().AlignCenter().Text(rol).FontSize(8).FontColor(ColorSecundario);
            });
        }

        // ---------------- Pie ----------------

        private static void Pie(IContainer contenedor, string? nota, DateTime generado)
        {
            contenedor.Column(columna =>
            {
                columna.Item().LineHorizontal(0.5f).LineColor(ColorBorde);
                if (!string.IsNullOrWhiteSpace(nota))
                {
                    columna.Item().PaddingTop(3).AlignCenter().Text(nota).FontSize(7.5f).FontColor(ColorSecundario);
                }

                columna.Item().PaddingTop(3).Row(fila =>
                {
                    fila.RelativeItem().Text($"Generado el {FechaHora(generado)}").FontSize(7.5f).FontColor(ColorSecundario);
                    fila.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(ColorSecundario));
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            });
        }

        // ---------------- Utilidades ----------------

        private static string Persona(PersonaConstancia p) =>
            string.IsNullOrWhiteSpace(p.Rango) ? p.Nombre : $"{p.Rango} {p.Nombre}";

        private static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy h:mm tt", Cultura);

        private static string LeerLogoPredeterminado()
        {
            var ensamblado = typeof(QuestPdfReportGenerator).Assembly;
            var recurso = ensamblado.GetManifestResourceNames().Single(n => n.EndsWith("logo-predeterminado.svg"));
            using var flujo = ensamblado.GetManifestResourceStream(recurso)!;
            using var lector = new StreamReader(flujo);
            return lector.ReadToEnd();
        }
    }
}
