using Domain.Entities.Configuracion;
using Domain.Entities.Historico;
using Domain.Entities.Taller;
using Domain.Enums;

namespace Infraestructure.Data.Seed.Demo
{
    /// <summary>
    /// Taller de ejemplo: proveedores, piezas con existencia (una bajo el mínimo), mecánicos militar y
    /// civil, un mantenimiento finalizado y la orden abierta del vehículo que está en mantenimiento.
    /// </summary>
    internal static class TallerDemoSeeder
    {
        public static int Seed(DatosDemo demo, PersonasDemo p, VehiculosDemo v)
        {
            var ahora = demo.Ahora;

            var repuestos = demo.Registrar(new Proveedor { Nombre = "Repuestos del Caribe, SRL", Rnc = "999000001", Telefono = "809-555-0101", Contacto = "Luis Peña" });
            var lubricantes = demo.Registrar(new Proveedor { Nombre = "Lubricantes Nacionales, SA", Rnc = "999000002", Telefono = "809-555-0102" });

            Pieza Pieza(string codigo, string nombre, string unidad, decimal minima, string? codigoBarras = null) =>
                demo.Registrar(new Pieza { Codigo = codigo, CodigoBarras = codigoBarras, Nombre = nombre, UnidadMedida = unidad, ExistenciaMinima = minima });

            var filtroAceite = Pieza("FIL-ACE-001", "Filtro de aceite", "Unidad", 4, "7501234567893");
            var aceite = Pieza("ACE-15W40", "Aceite de motor 15W-40", "Galón", 10, "0012345678905");
            var bandas = Pieza("FRE-BAN-010", "Juego de bandas de freno", "Juego", 2);
            var filtroAire = Pieza("FIL-AIR-002", "Filtro de aire", "Unidad", 3);

            MovimientoPieza Linea(Pieza pieza, decimal cantidad, decimal costo, DateTime fecha) => demo.Registrar(new MovimientoPieza
            {
                Concepto = ConceptoMovimientoPieza.Entrada,
                PiezaId = pieza.Id,
                Cantidad = cantidad,
                CostoUnitario = costo,
                Fecha = fecha
            });

            var fechaEntrada = ahora.AddDays(-20);
            demo.Registrar(new EntradaPieza
            {
                ProveedorId = repuestos.Id,
                NumeroFactura = "B0100009001",
                Fecha = fechaEntrada,
                Lineas = [Linea(filtroAceite, 6, 450, fechaEntrada), Linea(bandas, 2, 2800, fechaEntrada), Linea(filtroAire, 2, 900, fechaEntrada)]
            });
            demo.Registrar(new EntradaPieza
            {
                ProveedorId = lubricantes.Id,
                NumeroFactura = "B0100009002",
                Fecha = fechaEntrada,
                Lineas = [Linea(aceite, 20, 750, fechaEntrada)]
            });

            var militar = demo.Registrar(new Mecanico { PersonaId = p.ChoferDos.Id, Especialidad = "Motores y transmisión" });
            var personaCivil = demo.Registrar(new Persona
            {
                Identificacion = "999-0000099-9",
                Nombre = "Ramón Antonio",
                Apellido = "Taveras Gil",
                Sexo = SexoEnum.Masculino,
                TipoPersona = TipoPersona.Civil,
                Telefono = "809-555-0199"
            });
            var civil = demo.Registrar(new Mecanico { Persona = personaCivil, Especialidad = "Frenos y suspensión" });

            MovimientoPieza Consumo(SolicitudTaller orden, Pieza pieza, decimal cantidad, decimal costo, DateTime fecha) => demo.Registrar(new MovimientoPieza
            {
                Concepto = ConceptoMovimientoPieza.Consumo,
                PiezaId = pieza.Id,
                Cantidad = cantidad,
                CostoUnitario = costo,
                Fecha = fecha,
                SolicitudTaller = orden
            });

            // Mantenimiento finalizado (historial)
            var anio = ahora.Year;
            var ingreso = ahora.AddDays(-10);
            var finalizada = demo.Registrar(new SolicitudTaller
            {
                Numero = SolicitudTaller.FormatearNumero(anio, 1),
                Tipo = TipoSolicitudTaller.Mantenimiento,
                Estatus = EstatusSolicitudTaller.Finalizada,
                FechaSolicitud = ingreso.AddDays(-1),
                VehiculoId = v.HiluxUno.Id,
                SolicitantePersonaId = p.EncargadoFlotilla.Id,
                Motivo = "Mantenimiento preventivo de los 40,000 km.",
                Kilometraje = 40120,
                MecanicoId = militar.Id,
                FechaIngreso = ingreso,
                FechaSalida = ingreso.AddDays(1),
                Diagnostico = "Aceite y filtros al final de su vida útil.",
                TrabajoRealizado = "Cambio de aceite (2 gal), filtro de aceite y filtro de aire."
            });
            Consumo(finalizada, aceite, 2, 750, ingreso);
            Consumo(finalizada, filtroAceite, 1, 450, ingreso);
            Consumo(finalizada, filtroAire, 1, 900, ingreso);

            // Orden abierta: el vehículo en mantenimiento
            var enTaller = demo.Registrar(new SolicitudTaller
            {
                Numero = SolicitudTaller.FormatearNumero(anio, 2),
                Tipo = TipoSolicitudTaller.Reparacion,
                Estatus = EstatusSolicitudTaller.EnTaller,
                FechaSolicitud = ahora.AddDays(-2),
                VehiculoId = v.EnMantenimiento.Id,
                SolicitantePersonaId = p.EncargadoFlotilla.Id,
                Motivo = "Chirrido al frenar y vibración en el pedal.",
                Kilometraje = 152300,
                MecanicoId = civil.Id,
                FechaIngreso = ahora.AddDays(-1)
            });
            Consumo(enTaller, bandas, 1, 2800, ahora.AddDays(-1));

            demo.Context.Add(new Secuencia { Codigo = SolicitudTaller.CodigoSecuencia, Anio = anio, Ultimo = 2 });
            return 2;
        }
    }
}
