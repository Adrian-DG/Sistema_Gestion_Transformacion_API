using Domain.Constants;
using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using static Infraestructure.Data.Seed.CatalogoVehiculoSeeder;

namespace Infraestructure.Data.Seed
{
    /// <summary>Tipos de documento que se pueden adjuntar en cada flujo.</summary>
    public static class TipoDocumentoOperacionSeeder
    {
        private static readonly (Guid Operacion, Guid[] Documentos)[] Permitidos =
        [
            (TiposOperacion.Vehiculo, [TiposDocumento.Matricula, TiposDocumento.Poliza, TiposDocumento.Factura]),
            (TiposOperacion.Asignacion, [TiposDocumento.SolicitudVehiculo, TiposDocumento.Oficio]),
            (TiposOperacion.SolicitudTicket, [TiposDocumento.SolicitudTickets, TiposDocumento.Oficio]),
            (TiposOperacion.MovimientoCombustible, [TiposDocumento.Factura, TiposDocumento.Conduce]),
            (TiposOperacion.EntradaPieza, [TiposDocumento.Factura]),
            (TiposOperacion.CompraPieza, [TiposDocumento.ReciboCompra, TiposDocumento.Factura]),
            (TiposOperacion.SolicitudTaller, [TiposDocumento.Factura, TiposDocumento.OrdenServicio]),
        ];

        public static void Seed(this ModelBuilder builder)
        {
            builder.Entity<TipoDocumentoOperacion>().HasData(
                Permitidos.SelectMany(p => p.Documentos.Select(d => new TipoDocumentoOperacion
                {
                    TipoOperacionId = p.Operacion,
                    TipoDocumentoId = d
                })).ToArray());
        }
    }
}
