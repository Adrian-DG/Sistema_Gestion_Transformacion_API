using Application.Features.Vehiculo;
using AutoMapper;

namespace Application.Common.Profiles
{
    public class VehiculoProfile : Profile
    {
        public VehiculoProfile()
        {
            CreateMap<CreateVehiculoCommand, Domain.Entities.Recursos.Vehiculo>()
                .ForMember(dest => dest.Chasis, opt => opt.MapFrom(src => src.Chasis))
                .ForMember(dest => dest.Matricula, opt => opt.MapFrom(src => src.Matricula))
                .ForMember(dest => dest.Placa, opt => opt.MapFrom(src => src.Placa))
                .ForMember(dest => dest.Fabricacion, opt => opt.MapFrom(src => src.Fabricacion))
                .ForMember(dest => dest.MarcaId, opt => opt.MapFrom(src => src.MarcaId))
                .ForMember(dest => dest.ModeloId, opt => opt.MapFrom(src => src.ModeloId))
                .ForMember(dest => dest.TipoId, opt => opt.MapFrom(src => src.TipoId))
                .ForMember(dest => dest.ColorId, opt => opt.MapFrom(src => src.ColorId));
        }
    }
}
