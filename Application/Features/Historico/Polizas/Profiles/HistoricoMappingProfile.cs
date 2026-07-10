using Application.Features.Historico.Polizas;
using AutoMapper;
using Domain.Entities.Historico;

namespace Application.Features.Historico.Polizas.Profiles;

public class HistoricoMappingProfile : Profile
{
    public HistoricoMappingProfile()
    {
        CreateMap<Poliza, PolizaViewModel>();
    }
}
