using Application.Common.DTO;
using Application.Common.Response;
using Application.Contracts;
using Application.Features.Misc.ViewModels;
using Domain.Common;
using Domain.Common.Errors;
using Domain.Entities.Historico;
using Domain.Entities.Taller;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Taller;

public class MecanicoViewModel
{
    public Guid Id { get; set; }
    public Guid PersonaId { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    /// <summary>Militar, Asimilado o Civil.</summary>
    public string TipoPersona { get; set; } = string.Empty;
    public string? Rango { get; set; }
    public string? Telefono { get; set; }
    public string? Especialidad { get; set; }
    public bool Activo { get; set; }
    /// <summary>Solicitudes que tiene en el taller ahora mismo.</summary>
    public int SolicitudesEnTaller { get; set; }
}

/// <summary>Busca por nombre, cédula o especialidad.</summary>
public record FiltroMecanicosQuery : PaginationFilterQuery<MecanicoViewModel>
{
    public bool? Activo { get; set; }
}

public class FiltroMecanicosQueryValidator : AbstractValidator<FiltroMecanicosQuery>
{
    public FiltroMecanicosQueryValidator() => Include(new PaginationFilterQueryValidator<MecanicoViewModel>());
}

public class GetMecanicosQueryHandler(IUnitOfWork uow) : IRequestHandler<FiltroMecanicosQuery, Result<PagedData<MecanicoViewModel>>>
{
    public async Task<Result<PagedData<MecanicoViewModel>>> Handle(FiltroMecanicosQuery request, CancellationToken cancellationToken)
    {
        var solicitudes = uow.Query<SolicitudTaller>().Where(s => s.Estatus == EstatusSolicitudTaller.EnTaller);
        var query = uow.Query<Mecanico>();

        if (request.Activo is { } activo) query = query.Where(m => m.Status == activo);

        if (!string.IsNullOrWhiteSpace(request.Param))
        {
            var texto = request.Param.Trim();
            query = query.Where(m =>
                (m.Persona!.Nombre + " " + m.Persona.Apellido).Contains(texto) ||
                m.Persona.Identificacion.Contains(texto) ||
                (m.Especialidad != null && m.Especialidad.Contains(texto)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(m => m.Persona!.Apellido).ThenBy(m => m.Persona!.Nombre)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MecanicoViewModel
            {
                Id = m.Id,
                PersonaId = m.PersonaId,
                Identificacion = m.Persona!.Identificacion,
                NombreCompleto = m.Persona.Apellido + " " + m.Persona.Nombre,
                TipoPersona = m.Persona.TipoPersona.ToString(),
                Rango = m.Persona.Rango != null ? m.Persona.Rango.Nombre : null,
                Telefono = m.Persona.Telefono,
                Especialidad = m.Especialidad,
                Activo = m.Status,
                SolicitudesEnTaller = solicitudes.Count(s => s.MecanicoId == m.Id)
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedData<MecanicoViewModel>(rows, request.PageSize, request.PageNumber, total));
    }
}

/// <summary>Mecánicos activos para asignarlos al ingreso. Nombre: "Rango Apellido Nombre · Especialidad".</summary>
public record GetMecanicosCatalogoQuery : IRequest<Result<IReadOnlyList<CatalogItemViewModel>>>;

public class GetMecanicosCatalogoQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetMecanicosCatalogoQuery, Result<IReadOnlyList<CatalogItemViewModel>>>
{
    public async Task<Result<IReadOnlyList<CatalogItemViewModel>>> Handle(GetMecanicosCatalogoQuery request, CancellationToken cancellationToken)
    {
        var mecanicos = await uow.Query<Mecanico>()
            .Where(m => m.Status)
            .OrderBy(m => m.Persona!.Apellido).ThenBy(m => m.Persona!.Nombre)
            .Select(m => new CatalogItemViewModel
            {
                Id = m.Id,
                Nombre = (m.Persona!.Rango != null ? m.Persona.Rango.Nombre + " " : "")
                    + m.Persona.Apellido + " " + m.Persona.Nombre
                    + (m.Especialidad != null ? " · " + m.Especialidad : "")
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CatalogItemViewModel>>(mecanicos);
    }
}

/// <summary>Datos de un mecánico civil, que no está en SIPFFAA y se registra a mano.</summary>
public record MecanicoCivilRequest(string Identificacion, string Nombre, string Apellido, SexoEnum Sexo, string? Telefono);

/// <summary>
/// Registra un mecánico. Militares: <paramref name="PersonaId"/> de la persona importada desde SIPFFAA
/// (api/personas/sipffaa/{cedula}). Civiles: <paramref name="Civil"/> con sus datos; si ya hay una persona
/// con esa identificación se reutiliza.
/// </summary>
public record CreateMecanicoCommand(Guid? PersonaId, MecanicoCivilRequest? Civil, string? Especialidad) : IRequest<Result<Guid>>;

public class CreateMecanicoCommandValidator : AbstractValidator<CreateMecanicoCommand>
{
    public CreateMecanicoCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.PersonaId is { } id && id != Guid.Empty) ^ (x.Civil is not null))
            .WithMessage("Indique la persona de SIPFFAA o los datos del mecánico civil, no ambos.");
        RuleFor(x => x.Especialidad).MaximumLength(100);
        When(x => x.Civil is not null, () =>
        {
            RuleFor(x => x.Civil!.Identificacion).NotEmpty().MaximumLength(20).OverridePropertyName("Identificacion");
            RuleFor(x => x.Civil!.Nombre).NotEmpty().MaximumLength(100).OverridePropertyName("Nombre");
            RuleFor(x => x.Civil!.Apellido).NotEmpty().MaximumLength(100).OverridePropertyName("Apellido");
            RuleFor(x => x.Civil!.Sexo).IsInEnum().OverridePropertyName("Sexo");
            RuleFor(x => x.Civil!.Telefono).MaximumLength(20).OverridePropertyName("Telefono");
        });
    }
}

/// <summary>Actualiza la especialidad y el estado; <paramref name="Activo"/> en false lo desactiva.</summary>
public record UpdateMecanicoCommand(Guid Id, string? Especialidad, string? Telefono, bool Activo) : IRequest<Result>;

public class UpdateMecanicoCommandValidator : AbstractValidator<UpdateMecanicoCommand>
{
    public UpdateMecanicoCommandValidator()
    {
        RuleFor(x => x.Especialidad).MaximumLength(100);
        RuleFor(x => x.Telefono).MaximumLength(20);
    }
}

public class MecanicoCommandsHandler(IUnitOfWork uow) :
    IRequestHandler<CreateMecanicoCommand, Result<Guid>>,
    IRequestHandler<UpdateMecanicoCommand, Result>
{
    public async Task<Result<Guid>> Handle(CreateMecanicoCommand request, CancellationToken cancellationToken)
    {
        Persona? persona;
        if (request.PersonaId is { } personaId)
        {
            persona = await uow.Tracked<Persona>().FirstOrDefaultAsync(p => p.Id == personaId && p.Status, cancellationToken);
            if (persona is null) return Result.Failure<Guid>(PersonaErrors.NotFound(personaId));
        }
        else
        {
            var civil = request.Civil!;
            var identificacion = civil.Identificacion.Trim();
            persona = await uow.Tracked<Persona>().FirstOrDefaultAsync(p => p.Identificacion == identificacion, cancellationToken);
            if (persona is null)
            {
                persona = new Persona
                {
                    Id = Guid.NewGuid(),
                    Identificacion = identificacion,
                    Nombre = civil.Nombre.Trim(),
                    Apellido = civil.Apellido.Trim(),
                    Sexo = civil.Sexo,
                    TipoPersona = TipoPersona.Civil,
                    Telefono = string.IsNullOrWhiteSpace(civil.Telefono) ? null : civil.Telefono.Trim(),
                    Status = true
                };
                await uow.AddAsync(persona, cancellationToken);
            }
        }

        if (await uow.Query<Mecanico>().AnyAsync(m => m.PersonaId == persona.Id, cancellationToken))
        {
            return Result.Failure<Guid>(MecanicoErrors.Duplicado(persona.Identificacion));
        }

        var mecanico = new Mecanico
        {
            PersonaId = persona.Id,
            Especialidad = string.IsNullOrWhiteSpace(request.Especialidad) ? null : request.Especialidad.Trim(),
            Status = true
        };

        await uow.AddAsync(mecanico, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(mecanico.Id);
    }

    public async Task<Result> Handle(UpdateMecanicoCommand request, CancellationToken cancellationToken)
    {
        var mecanico = await uow.Tracked<Mecanico>()
            .Include(m => m.Persona)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (mecanico is null) return Result.Failure(MecanicoErrors.NotFound(request.Id));

        mecanico.Especialidad = string.IsNullOrWhiteSpace(request.Especialidad) ? null : request.Especialidad.Trim();
        mecanico.Persona!.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        mecanico.Status = request.Activo;

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
