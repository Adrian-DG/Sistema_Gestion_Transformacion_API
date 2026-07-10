using Application.Features.Misc.ViewModels;
using AutoMapper;
using Domain.Entities.Misc;

namespace Application.Features.Misc;

public class MiscMappingProfile : Profile
{
    public MiscMappingProfile()
    {
        CreateMap<Aseguradora, CatalogItemViewModel>();
        CreateMap<Color, CatalogItemViewModel>();
        CreateMap<Marca, CatalogItemViewModel>();
        CreateMap<TipoDocumento, CatalogItemViewModel>();
        CreateMap<TipoOperacion, CatalogItemViewModel>();
        CreateMap<TipoVehiculo, CatalogItemViewModel>();
        CreateMap<Modelo, ModeloViewModel>();
        CreateMap<Rango, RangoViewModel>();
    }
}
