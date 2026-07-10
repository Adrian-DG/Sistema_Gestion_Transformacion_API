using Application.Features.Historico.ViewModels;
using AutoMapper;
using Domain.Entities.Historico;

namespace Application.Features.Historico;

public class HistoricoMappingProfile : Profile
{
    public HistoricoMappingProfile()
    {
        CreateMap<Poliza, PolizaViewModel>();
    }
}
