using Application.Features.Vehiculo;
using AutoMapper;

namespace Application.Common.Profiles
{
    public class VehiculoProfile : Profile
    {
        public VehiculoProfile()
        {
            // El ingreso del vehículo (CreateVehiculoCommand) se arma en su handler con VehiculoReglas.Aplicar.
        }
    }
}
