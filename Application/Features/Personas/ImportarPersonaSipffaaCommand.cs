using Application.Contracts;
using Application.Contracts.Externos;
using Domain.Common;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Personas;

/// <summary>
/// Devuelve el Id de la persona con esa cédula. Si no está registrada, la crea con los datos del miembro
/// en SIPFFAA (nombre, sexo, rango, institución y departamento). Las solicitudes guardan personas del
/// sistema; así se pueden registrar a partir de lo elegido en el buscador de SIPFFAA.
/// </summary>
public record ImportarPersonaSipffaaCommand(string Cedula) : IRequest<Result<Guid>>;

public class ImportarPersonaSipffaaCommandValidator : AbstractValidator<ImportarPersonaSipffaaCommand>
{
    public ImportarPersonaSipffaaCommandValidator()
    {
        RuleFor(x => x.Cedula)
            .NotEmpty()
            .Matches(@"^\d{3}-?\d{7}-?\d$")
            .WithMessage("La cédula debe tener el formato 000-0000000-0.");
    }
}

public class ImportarPersonaSipffaaCommandHandler(IUnitOfWork uow, ISipffaaService sipffaa)
    : IRequestHandler<ImportarPersonaSipffaaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ImportarPersonaSipffaaCommand request, CancellationToken cancellationToken)
    {
        var digitos = request.Cedula.Replace("-", string.Empty).Trim();
        var cedula = $"{digitos[..3]}-{digitos[3..10]}-{digitos[10..]}";

        // Puede haberse registrado con o sin guiones.
        var existente = await uow.Query<Persona>()
            .Where(p => p.Identificacion == cedula || p.Identificacion == digitos)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existente is { } id) return Result.Success(id);

        var miembro = await sipffaa.GetMiembroByCedulaAsync(digitos, cancellationToken);
        if (miembro is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "Sipffaa.MiembroNotFound", $"No se encontró un miembro con la cédula '{cedula}' en SIPFFAA."));
        }

        var persona = new Persona
        {
            Identificacion = cedula,
            Nombre = Unir(miembro.PrimerNombre, miembro.SegundoNombre),
            Apellido = Unir(miembro.PrimerApellido, miembro.SegundoApellido),
            Sexo = miembro.Sexo?.Trim().StartsWith('F') == true ? SexoEnum.Femenino : SexoEnum.Masculino,
            TipoPersona = miembro.EsMilitar ? TipoPersona.Militar : TipoPersona.Civil,
            RangoId = miembro.EsMilitar ? await RangoIdAsync(miembro.Rango, cancellationToken) : null,
            Institucion = Enum.TryParse<InstitucionEnum>(miembro.SiglasInstitucion?.Trim(), true, out var institucion)
                ? institucion
                : null,
            CodDependencia = miembro.CodDepartamento > 0 ? miembro.CodDepartamento : null,
            Dependencia = string.IsNullOrWhiteSpace(miembro.Departamento) ? null : miembro.Departamento.Trim(),
            Status = true
        };

        await uow.AddAsync(persona, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(persona.Id);
    }

    /// <summary>El rango de SIPFFAA se asocia al catálogo local por su nombre (o su denominación en la Armada).</summary>
    private async Task<Guid?> RangoIdAsync(string? rango, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rango)) return null;
        var nombre = rango.Trim();

        return await uow.Query<Rango>()
            .Where(r => r.Status && (r.Nombre == nombre || r.NombreArmada == nombre))
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string Unir(params string?[] partes) =>
        string.Join(" ", partes.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
}
