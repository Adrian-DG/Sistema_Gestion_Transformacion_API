using Application.Features.Vehiculo;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

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

            CreateMap<Domain.Entities.Recursos.Vehiculo, VehiculoViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Chasis, opt => opt.MapFrom(src => src.Chasis))
                .ForMember(dest => dest.Matricula, opt => opt.MapFrom(src => src.Matricula))
                .ForMember(dest => dest.Placa, opt => opt.MapFrom(src => src.Placa))
                .ForMember(dest => dest.Marca, opt => opt.MapFrom(src => src.Marca!.Nombre))
                .ForMember(dest => dest.Modelo, opt => opt.MapFrom(src => src.Modelo!.Nombre))
                .ForMember(dest => dest.Fabricacion, opt => opt.MapFrom(src => src.Fabricacion))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color!.Nombre));
        }
    }
}
