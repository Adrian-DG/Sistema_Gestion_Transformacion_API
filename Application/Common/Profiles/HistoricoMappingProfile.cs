using Application.Common.ViewModels;
using AutoMapper;
using Domain.Entities.Historico;

namespace Application.Common.Profiles;

public class HistoricoMappingProfile : Profile
{
    public HistoricoMappingProfile()
    {
        CreateMap<Poliza, PolizaViewModel>();
    }
}
