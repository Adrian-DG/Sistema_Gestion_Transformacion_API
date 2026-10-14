using Domain.Entities.Historico;
using Domain.Entities.Recursos;
using static Infraestructure.Data.Seed.CatalogoVehiculoSeeder;

namespace Infraestructure.Data.Seed.Demo
{
    /// <summary>
    /// Pólizas vigentes, por vencer (30 días o menos), vencidas y una renovación, con fechas relativas
    /// al momento de la carga para que siempre haya casos de cada estado de cobertura.
    /// </summary>
    internal static class PolizaDemoSeeder
    {
        public static int Seed(DatosDemo demo, VehiculosDemo v)
        {
            var hoy = demo.Hoy;
            var cantidad = 0;

            void Poliza(Vehiculo vehiculo, Guid aseguradoraId, DateOnly efectividad, int meses = 12)
            {
                cantidad++;
                demo.Registrar(new Poliza
                {
                    Numero = $"POL-DEMO-{cantidad:D5}",
                    FechaExpedicion = efectividad.AddDays(-5),
                    FechaEfectividad = efectividad,
                    FechaVencimiento = efectividad.AddMonths(meses),
                    VehiculoId = vehiculo.Id,
                    AseguradoraId = aseguradoraId
                });
            }

            // Vigentes
            Poliza(v.HiluxUno, Aseguradoras.Reservas, hoy.AddMonths(-2));
            Poliza(v.Colorado, Aseguradoras.Universal, hoy.AddMonths(-1));
            Poliza(v.Wrangler, Aseguradoras.Mapfre, hoy.AddMonths(-6));

            // Por vencer (vencen dentro de los próximos 30 días)
            Poliza(v.HiluxDos, Aseguradoras.Reservas, hoy.AddMonths(-12).AddDays(10));
            Poliza(v.Frontier, Aseguradoras.Humano, hoy.AddMonths(-12).AddDays(25));

            // Vencida sin renovar
            Poliza(v.L200, Aseguradoras.Universal, hoy.AddMonths(-13));

            // Vencida y renovada: el historial del vehículo muestra ambas
            Poliza(v.Ranger, Aseguradoras.Mapfre, hoy.AddMonths(-14));
            Poliza(v.Ranger, Aseguradoras.Reservas, hoy.AddMonths(-2));

            // El vehículo fuera de servicio queda sin póliza.
            return cantidad;
        }
    }
}
