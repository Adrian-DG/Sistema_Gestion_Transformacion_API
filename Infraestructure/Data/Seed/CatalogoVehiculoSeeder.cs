using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data.Seed
{
    /// <summary>
    /// Catálogos necesarios para registrar vehículos, pólizas y documentos: colores, tipos de vehículo,
    /// aseguradoras y tipos de documento. Ids fijos y Status activo, como el resto de catálogos.
    /// </summary>
    public static class CatalogoVehiculoSeeder
    {
        public static class Colores
        {
            public static readonly Guid Blanco = new("4c8e1a63-7d5f-4192-a034-8f0000000001");
            public static readonly Guid Negro = new("4c8e1a63-7d5f-4192-a034-8f0000000002");
            public static readonly Guid Gris = new("4c8e1a63-7d5f-4192-a034-8f0000000003");
            public static readonly Guid Plateado = new("4c8e1a63-7d5f-4192-a034-8f0000000004");
            public static readonly Guid VerdeOlivo = new("4c8e1a63-7d5f-4192-a034-8f0000000005");
            public static readonly Guid Azul = new("4c8e1a63-7d5f-4192-a034-8f0000000006");
            public static readonly Guid Rojo = new("4c8e1a63-7d5f-4192-a034-8f0000000007");
        }

        public static class TiposVehiculo
        {
            public static readonly Guid Camioneta = new("5d9f2b74-8e60-42a3-b145-900000000001");
            public static readonly Guid Jeepeta = new("5d9f2b74-8e60-42a3-b145-900000000002");
            public static readonly Guid Automovil = new("5d9f2b74-8e60-42a3-b145-900000000003");
            public static readonly Guid Minibus = new("5d9f2b74-8e60-42a3-b145-900000000004");
            public static readonly Guid Autobus = new("5d9f2b74-8e60-42a3-b145-900000000005");
            public static readonly Guid Camion = new("5d9f2b74-8e60-42a3-b145-900000000006");
            public static readonly Guid Motocicleta = new("5d9f2b74-8e60-42a3-b145-900000000007");
        }

        public static class Aseguradoras
        {
            public static readonly Guid Reservas = new("6e0a3c85-9f71-43b4-c256-a10000000001");
            public static readonly Guid Universal = new("6e0a3c85-9f71-43b4-c256-a10000000002");
            public static readonly Guid Mapfre = new("6e0a3c85-9f71-43b4-c256-a10000000003");
            public static readonly Guid Humano = new("6e0a3c85-9f71-43b4-c256-a10000000004");
        }

        public static class TiposDocumento
        {
            public static readonly Guid Oficio = new("7f1b4d96-a082-44c5-d367-b20000000001");
            public static readonly Guid SolicitudVehiculo = new("7f1b4d96-a082-44c5-d367-b20000000002");
            public static readonly Guid SolicitudTickets = new("7f1b4d96-a082-44c5-d367-b20000000003");
            public static readonly Guid Matricula = new("7f1b4d96-a082-44c5-d367-b20000000004");
            public static readonly Guid Poliza = new("7f1b4d96-a082-44c5-d367-b20000000005");
            public static readonly Guid Factura = new("7f1b4d96-a082-44c5-d367-b20000000006");
        }

        public static void Seed(this ModelBuilder builder)
        {
            builder.Entity<Color>().HasData(
                new Color { Id = Colores.Blanco, Nombre = "Blanco", Status = true },
                new Color { Id = Colores.Negro, Nombre = "Negro", Status = true },
                new Color { Id = Colores.Gris, Nombre = "Gris", Status = true },
                new Color { Id = Colores.Plateado, Nombre = "Plateado", Status = true },
                new Color { Id = Colores.VerdeOlivo, Nombre = "Verde olivo", Status = true },
                new Color { Id = Colores.Azul, Nombre = "Azul", Status = true },
                new Color { Id = Colores.Rojo, Nombre = "Rojo", Status = true });

            builder.Entity<TipoVehiculo>().HasData(
                new TipoVehiculo { Id = TiposVehiculo.Camioneta, Nombre = "Camioneta", Status = true },
                new TipoVehiculo { Id = TiposVehiculo.Jeepeta, Nombre = "Jeepeta", Status = true },
                new TipoVehiculo { Id = TiposVehiculo.Automovil, Nombre = "Automóvil", Status = true },
                new TipoVehiculo { Id = TiposVehiculo.Minibus, Nombre = "Minibús", Status = true },
                new TipoVehiculo { Id = TiposVehiculo.Autobus, Nombre = "Autobús", Status = true },
                new TipoVehiculo { Id = TiposVehiculo.Camion, Nombre = "Camión", Status = true },
                new TipoVehiculo { Id = TiposVehiculo.Motocicleta, Nombre = "Motocicleta", Status = true });

            builder.Entity<Aseguradora>().HasData(
                new Aseguradora { Id = Aseguradoras.Reservas, Nombre = "Seguros Reservas", Status = true },
                new Aseguradora { Id = Aseguradoras.Universal, Nombre = "Seguros Universal", Status = true },
                new Aseguradora { Id = Aseguradoras.Mapfre, Nombre = "Mapfre BHD Seguros", Status = true },
                new Aseguradora { Id = Aseguradoras.Humano, Nombre = "Humano Seguros", Status = true });

            builder.Entity<TipoDocumento>().HasData(
                new TipoDocumento { Id = TiposDocumento.Oficio, Nombre = "Oficio", Status = true },
                new TipoDocumento { Id = TiposDocumento.SolicitudVehiculo, Nombre = "Solicitud de vehículo", Status = true },
                new TipoDocumento { Id = TiposDocumento.SolicitudTickets, Nombre = "Solicitud de tickets de combustible", Status = true },
                new TipoDocumento { Id = TiposDocumento.Matricula, Nombre = "Matrícula", Status = true },
                new TipoDocumento { Id = TiposDocumento.Poliza, Nombre = "Póliza de seguro", Status = true },
                new TipoDocumento { Id = TiposDocumento.Factura, Nombre = "Factura", Status = true });
        }
    }
}
