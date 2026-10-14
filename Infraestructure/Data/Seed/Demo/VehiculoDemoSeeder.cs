using Domain.Entities.Recursos;
using static Infraestructure.Data.Seed.CatalogoVehiculoSeeder;

namespace Infraestructure.Data.Seed.Demo
{
    /// <summary>Flotilla de ejemplo; <see cref="FueraDeServicio"/> no está disponible para asignaciones.</summary>
    internal sealed record VehiculosDemo(
        Vehiculo HiluxUno,
        Vehiculo HiluxDos,
        Vehiculo Ranger,
        Vehiculo Frontier,
        Vehiculo Colorado,
        Vehiculo Wrangler,
        Vehiculo L200,
        Vehiculo FueraDeServicio)
    {
        public IReadOnlyList<Vehiculo> Todos => [HiluxUno, HiluxDos, Ranger, Frontier, Colorado, Wrangler, L200, FueraDeServicio];
    }

    internal static class VehiculoDemoSeeder
    {
        public static VehiculosDemo Seed(DatosDemo demo)
        {
            var secuencia = 0;

            Vehiculo Vehiculo(Guid marcaId, Guid modeloId, Guid tipoId, Guid colorId, int fabricacion, bool disponible = true)
            {
                secuencia++;
                return demo.Registrar(new Vehiculo
                {
                    Chasis = $"DEMO{fabricacion}{secuencia:D9}",
                    Matricula = $"MAT-DEMO-{secuencia:D4}",
                    Placa = $"OD{secuencia:D5}",
                    Fabricacion = fabricacion,
                    MarcaId = marcaId,
                    ModeloId = modeloId,
                    TipoId = tipoId,
                    ColorId = colorId,
                    Disponible = disponible
                });
            }

            return new VehiculosDemo(
                HiluxUno: Vehiculo(MarcaSeeder.Toyota, ModeloSeeder.Hilux, TiposVehiculo.Camioneta, Colores.Blanco, 2023),
                HiluxDos: Vehiculo(MarcaSeeder.Toyota, ModeloSeeder.Hilux, TiposVehiculo.Camioneta, Colores.VerdeOlivo, 2022),
                Ranger: Vehiculo(MarcaSeeder.Ford, ModeloSeeder.Ranger, TiposVehiculo.Camioneta, Colores.Gris, 2021),
                Frontier: Vehiculo(MarcaSeeder.Nissan, ModeloSeeder.Frontier, TiposVehiculo.Camioneta, Colores.Plateado, 2020),
                Colorado: Vehiculo(MarcaSeeder.Chevrolet, ModeloSeeder.Colorado, TiposVehiculo.Camioneta, Colores.Negro, 2024),
                Wrangler: Vehiculo(MarcaSeeder.Jeep, ModeloSeeder.Wrangler, TiposVehiculo.Jeepeta, Colores.Negro, 2023),
                L200: Vehiculo(MarcaSeeder.Mitsubishi, ModeloSeeder.L200, TiposVehiculo.Camioneta, Colores.Blanco, 2019),
                FueraDeServicio: Vehiculo(MarcaSeeder.Mazda, ModeloSeeder.BT50, TiposVehiculo.Camioneta, Colores.Azul, 2016, disponible: false));
        }
    }
}
