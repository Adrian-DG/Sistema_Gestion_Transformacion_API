using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts.Usuarios;
using Domain.Common;
using FluentValidation;
using MediatR;

namespace Application.Features.Usuarios;

/// <param name="Activo">Nulo para listar todos.</param>
public record GetUsuariosQuery(int PageNumber, int PageSize, string? Param, bool? Activo) : IRequest<Result<PagedData<UsuarioViewModel>>>;

public record GetUsuarioByIdQuery(Guid Id) : IRequest<Result<UsuarioDetalleViewModel>>;

public record GetRolesQuery : IRequest<Result<IReadOnlyList<RolViewModel>>>;

public class GetUsuariosQueryValidator : AbstractValidator<GetUsuariosQuery>
{
    public GetUsuariosQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("El número de página debe ser mayor que 0.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("El tamaño de página debe estar entre 1 y 200.");
    }
}

public class UsuarioQueryHandler(IUsuarioService usuarios) :
    IRequestHandler<GetUsuariosQuery, Result<PagedData<UsuarioViewModel>>>,
    IRequestHandler<GetUsuarioByIdQuery, Result<UsuarioDetalleViewModel>>,
    IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RolViewModel>>>
{
    public async Task<Result<PagedData<UsuarioViewModel>>> Handle(GetUsuariosQuery request, CancellationToken cancellationToken)
    {
        var filtro = new PaginationFilterQuery<UsuarioViewModel>(request.PageNumber, request.PageSize, request.Param ?? string.Empty);
        return Result.Success(await usuarios.GetAsync(filtro, request.Activo, cancellationToken));
    }

    public Task<Result<UsuarioDetalleViewModel>> Handle(GetUsuarioByIdQuery request, CancellationToken cancellationToken) =>
        usuarios.GetByIdAsync(request.Id, cancellationToken);

    public async Task<Result<IReadOnlyList<RolViewModel>>> Handle(GetRolesQuery request, CancellationToken cancellationToken) =>
        Result.Success(await usuarios.GetRolesAsync(cancellationToken));
}
