using Application.Contracts;
using Application.Contracts.Authentication;
using Domain.Common;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Authentication;

public record LoginUserCommand(string Username, string Password) : IRequest<Result<string>>;

public class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede exceder los 50 caracteres.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
    }
}

public class LoginUserHandler(IUnitOfWork uow) : IRequestHandler<LoginUserCommand, Result<string>>
{    
    public async Task<Result<string>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        return await uow.AuthenticationRepository.Login(request.Username, request.Password);
    }
}