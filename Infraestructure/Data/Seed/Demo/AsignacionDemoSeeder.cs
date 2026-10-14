using Domain.Constants;
using Domain.Entities.Historico;
using Domain.Entities.Recursos;
using Domain.Enums;
using System.Text;
using static Infraestructure.Data.Seed.Demo.SipffaaDemo;

namespace Infraestructure.Data.Seed.Demo
{
    /// <summary>
    /// Asignaciones en todos los estatus, con fechas relativas al momento de la carga para que siempre
    /// haya una en curso, una próxima, una vencida sin retorno tardío, etc. Las aprobadas no se solapan
    /// en el mismo vehículo, igual que exige el sistema.
    /// </summary>
    internal static class AsignacionDemoSeeder
    {
        public static int Seed(DatosDemo demo, PersonasDemo p, VehiculosDemo v)
        {
            var ahora = demo.Ahora;
            var cantidad = 0;

            Asignacion Asignar(
                Persona solicitante, Item cargoSolicitante, Item departamento,
                Persona asignado, Item? cargoAsignado, Persona chofer, Vehiculo vehiculo,
                string proposito, string destino, DateTime salida, DateTime retornoEstimado)
            {
                cantidad++;
                var asignacion = demo.Registrar(new Asignacion
                {
                    Id = Guid.NewGuid(),
                    Estatus = EstatusSolicitud.Pendiente,
                    SolicitantePersonaId = solicitante.Id,
                    RangoSolicitante = p.RangoDe(solicitante),
                    CodCargoSolicitante = cargoSolicitante.Codigo,
                    CargoSolicitante = cargoSolicitante.Nombre,
                    CodDependenciaSolicitante = departamento.Codigo,
                    DependenciaSolicitante = departamento.Nombre,
                    TipoAsignacion = cargoAsignado is null ? TipoBeneficiario.Persona : TipoBeneficiario.Cargo,
                    PersonaId = asignado.Id,
                    CodCargo = cargoAsignado?.Codigo,
                    Cargo = cargoAsignado?.Nombre,
                    ChoferPersonaId = chofer.Id,
                    RangoChofer = p.RangoDe(chofer),
                    VehiculoId = vehiculo.Id,
                    Proposito = proposito,
                    Destino = destino,
                    FechaSalida = salida,
                    FechaRetornoEstimada = retornoEstimado
                });

                demo.Registrar(new Adjunto
                {
                    File = DocumentoDemo.Pdf($"Solicitud de vehículo {vehiculo.Placa}", proposito),
                    NombreArchivo = $"solicitud-vehiculo-{cantidad:D3}.pdf",
                    ContentType = "application/pdf",
                    TipoDocumentoId = CatalogoVehiculoSeeder.TiposDocumento.SolicitudVehiculo,
                    TipoOperacionId = TiposOperacion.Asignacion,
                    IdentificadorOperacion = asignacion.Id
                });

                return asignacion;
            }

            void Aprobar(Asignacion a, DateTime fecha) => a.Aprobar(demo.UsuarioId, fecha);

            // Finalizada a tiempo, asignación personal (el solicitante es el mismo asignado).
            var finalizada = Asignar(p.EncargadoNomina, Cargos.EncargadoNomina, Departamentos.RecursosHumanos,
                p.EncargadoNomina, null, p.ChoferCivil, v.Ranger,
                "Traslado de expedientes de personal a la Sede Central.", "Santiago de los Caballeros",
                ahora.AddDays(-20).Date.AddHours(7), ahora.AddDays(-18).Date.AddHours(18));
            Aprobar(finalizada, ahora.AddDays(-21));
            finalizada.RegistrarRetorno(ahora.AddDays(-18).Date.AddHours(16));

            // Finalizada con retorno tardío, asignación al cargo.
            var tardia = Asignar(p.DirectorTic, Cargos.DirectorTic, Departamentos.Tic,
                p.DirectorTic, Cargos.DirectorTic, p.ChoferDos, v.Frontier,
                "Instalación de enlaces de comunicación en destacamentos fronterizos.", "Dajabón",
                ahora.AddDays(-12).Date.AddHours(6), ahora.AddDays(-9).Date.AddHours(18));
            Aprobar(tardia, ahora.AddDays(-13));
            tardia.RegistrarRetorno(ahora.AddDays(-8).Date.AddHours(11));

            // Aprobada y en curso: vehículo al cargo del Director de Transportación.
            var enCurso = Asignar(p.SubdirectorTransportacion, Cargos.SubdirectorTransportacion, Departamentos.Transportacion,
                p.DirectorTransportacion, Cargos.DirectorTransportacion, p.ChoferUno, v.HiluxUno,
                "Vehículo asignado al Director de Transportación para funciones del cargo.", "Santo Domingo",
                ahora.AddDays(-1).Date.AddHours(8), ahora.AddDays(30).Date.AddHours(17));
            Aprobar(enCurso, ahora.AddDays(-2));

            // Aprobada con retorno vencido (debe aparecer como vencida sin retorno en el reporte).
            var vencida = Asignar(p.AyudanteMinistro, Cargos.Ayudante, Departamentos.Despacho,
                p.AyudanteMinistro, null, p.ChoferDos, v.Wrangler,
                "Escolta de la comitiva del Ministro en actividad oficial.", "San Pedro de Macorís",
                ahora.AddDays(-4).Date.AddHours(9), ahora.AddDays(-2).Date.AddHours(17));
            Aprobar(vencida, ahora.AddDays(-5));

            // Aprobada para una salida próxima.
            var proxima = Asignar(p.EncargadoFlotilla, Cargos.EncargadoFlotilla, Departamentos.Transportacion,
                p.EncargadoFlotilla, null, p.ChoferCivil, v.Colorado,
                "Inspección de vehículos asignados a unidades del interior.", "La Vega",
                ahora.AddDays(3).Date.AddHours(7), ahora.AddDays(4).Date.AddHours(18));
            Aprobar(proxima, ahora.AddDays(-1));

            // Pendiente de aprobación.
            Asignar(p.DirectorTic, Cargos.DirectorTic, Departamentos.Tic,
                p.DirectorTic, null, p.ChoferUno, v.HiluxDos,
                "Traslado de equipos de cómputo a la Dirección Regional Norte.", "Puerto Plata",
                ahora.AddDays(5).Date.AddHours(7), ahora.AddDays(6).Date.AddHours(18));

            // Rechazada.
            var rechazada = Asignar(p.EncargadoNomina, Cargos.EncargadoNomina, Departamentos.RecursosHumanos,
                p.EncargadoNomina, null, p.ChoferCivil, v.L200,
                "Actividad recreativa del personal.", "Juan Dolio",
                ahora.AddDays(7).Date.AddHours(8), ahora.AddDays(7).Date.AddHours(20));
            rechazada.Rechazar("La actividad no corresponde a una misión oficial.");

            // Cancelada por error de digitación.
            var cancelada = Asignar(p.AyudanteMinistro, Cargos.Ayudante, Departamentos.Despacho,
                p.AyudanteMinistro, null, p.ChoferUno, v.Ranger,
                "Traslado de documentos al Palacio Nacional.", "Santo Domingo",
                ahora.AddDays(2).Date.AddHours(9), ahora.AddDays(2).Date.AddHours(13));
            cancelada.Cancelar("Se registró con el vehículo equivocado; se solicitará nuevamente.");

            return cantidad;
        }
    }

    /// <summary>Documento PDF mínimo (una página con dos líneas de texto) para los adjuntos de ejemplo.</summary>
    internal static class DocumentoDemo
    {
        public static byte[] Pdf(string titulo, string texto)
        {
            static string Escapar(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

            var contenido = $"BT /F1 16 Tf 72 720 Td ({Escapar(titulo)}) Tj /F1 11 Tf 0 -28 Td ({Escapar(texto)}) Tj " +
                            "0 -20 Td (Documento de ejemplo - datos de demostracion) Tj ET";

            string[] objetos =
            [
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
                $"<< /Length {Encoding.Latin1.GetByteCount(contenido)} >>\nstream\n{contenido}\nendstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
            ];

            var pdf = new StringBuilder("%PDF-1.4\n");
            var posiciones = new List<int>();
            for (var i = 0; i < objetos.Length; i++)
            {
                posiciones.Add(Encoding.Latin1.GetByteCount(pdf.ToString()));
                pdf.Append($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
            }

            var xref = Encoding.Latin1.GetByteCount(pdf.ToString());
            pdf.Append($"xref\n0 {objetos.Length + 1}\n0000000000 65535 f \n");
            foreach (var posicion in posiciones)
            {
                pdf.Append($"{posicion:D10} 00000 n \n");
            }
            pdf.Append($"trailer\n<< /Size {objetos.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

            return Encoding.Latin1.GetBytes(pdf.ToString());
        }
    }
}
