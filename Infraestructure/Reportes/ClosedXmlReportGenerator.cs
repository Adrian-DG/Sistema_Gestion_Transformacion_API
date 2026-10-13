using Application.Contracts.Reportes;
using ClosedXML.Excel;

namespace Infraestructure.Reportes
{
    public class ClosedXmlReportGenerator : IExcelReportGenerator
    {
        private const int FilaEncabezados = 5;
        private const int MaxAnchoColumna = 60;
        private const string FormatoNumero = "#,##0.00";
        private const string FormatoEntero = "#,##0";
        private const string FormatoFecha = "dd/MM/yyyy";
        private const string FormatoFechaHora = "dd/MM/yyyy HH:mm";

        public byte[] Generar(LibroReporte libro)
        {
            using var workbook = new XLWorkbook();

            for (var i = 0; i < libro.Hojas.Count; i++)
            {
                AgregarHoja(workbook, libro, libro.Hojas[i], i);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void AgregarHoja(XLWorkbook workbook, LibroReporte libro, HojaReporte hoja, int indice)
        {
            var ws = workbook.Worksheets.Add(NombreHoja(hoja.Nombre));
            var columnas = hoja.Encabezados.Count;

            ws.Cell(1, 1).Value = libro.Titulo;
            ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
            ws.Cell(2, 1).Value = libro.Subtitulo;
            ws.Cell(2, 1).Style.Font.SetItalic();
            ws.Cell(3, 1).Value = hoja.Titulo;
            ws.Cell(3, 1).Style.Font.SetBold();

            for (var c = 0; c < columnas; c++)
            {
                ws.Cell(FilaEncabezados, c + 1).Value = hoja.Encabezados[c];
            }

            if (hoja.Filas.Count == 0)
            {
                ws.Range(FilaEncabezados, 1, FilaEncabezados, columnas).Style.Font.SetBold();
                ws.Cell(FilaEncabezados + 1, 1).Value = "Sin datos para el período seleccionado.";
                ws.Cell(FilaEncabezados + 1, 1).Style.Font.SetItalic();
                ws.Columns(1, columnas).AdjustToContents(FilaEncabezados, FilaEncabezados + 1);
                return;
            }

            for (var f = 0; f < hoja.Filas.Count; f++)
            {
                var fila = hoja.Filas[f];
                for (var c = 0; c < columnas && c < fila.Length; c++)
                {
                    EscribirCelda(ws.Cell(FilaEncabezados + 1 + f, c + 1), fila[c]);
                }
            }

            var ultimaFila = FilaEncabezados + hoja.Filas.Count;
            var tabla = ws.Range(FilaEncabezados, 1, ultimaFila, columnas).CreateTable($"Tabla{indice + 1}");
            tabla.Theme = XLTableTheme.TableStyleMedium2;

            if (hoja.ColumnasTotalizables is { Count: > 0 } totalizables)
            {
                tabla.ShowTotalsRow = true;
                tabla.Field(0).TotalsRowLabel = "Total";
                foreach (var columna in totalizables.Where(c => c > 0 && c < columnas))
                {
                    tabla.Field(columna).TotalsRowFunction = XLTotalsRowFunction.Sum;
                    var formato = EsColumnaEntera(hoja, columna) ? FormatoEntero : FormatoNumero;
                    tabla.Field(columna).TotalsCell.Style.NumberFormat.Format = formato;
                }
            }

            ws.SheetView.FreezeRows(FilaEncabezados);
            ws.Columns(1, columnas).AdjustToContents(FilaEncabezados, tabla.LastRow().RowNumber());
            foreach (var columna in ws.Columns(1, columnas))
            {
                if (columna.Width > MaxAnchoColumna)
                {
                    columna.Width = MaxAnchoColumna;
                    columna.Style.Alignment.WrapText = true;
                }
            }
        }

        private static void EscribirCelda(IXLCell cell, object? valor)
        {
            switch (valor)
            {
                case null:
                    break;
                case DateOnly fecha:
                    cell.Value = fecha.ToDateTime(TimeOnly.MinValue);
                    cell.Style.NumberFormat.Format = FormatoFecha;
                    break;
                case DateTime fechaHora:
                    cell.Value = fechaHora;
                    cell.Style.NumberFormat.Format = fechaHora.TimeOfDay == TimeSpan.Zero ? FormatoFecha : FormatoFechaHora;
                    break;
                case decimal numero:
                    cell.Value = numero;
                    cell.Style.NumberFormat.Format = FormatoNumero;
                    break;
                case double numero:
                    cell.Value = numero;
                    cell.Style.NumberFormat.Format = FormatoNumero;
                    break;
                case int entero:
                    cell.Value = entero;
                    cell.Style.NumberFormat.Format = FormatoEntero;
                    break;
                case bool booleano:
                    cell.Value = booleano ? "Sí" : "No";
                    break;
                default:
                    cell.Value = valor.ToString();
                    break;
            }
        }

        private static bool EsColumnaEntera(HojaReporte hoja, int columna) =>
            hoja.Filas.All(f => columna >= f.Length || f[columna] is null or int);

        /// <summary>Excel limita los nombres de hoja a 31 caracteres y prohíbe algunos símbolos.</summary>
        private static string NombreHoja(string nombre)
        {
            var limpio = new string(nombre.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray());
            return limpio.Length > 31 ? limpio[..31] : limpio;
        }
    }
}
