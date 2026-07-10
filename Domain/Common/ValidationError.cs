namespace Domain.Common;

public sealed class ValidationError : Error
{
    public Error[] Errors { get; }

    private ValidationError(Error[] errors)
        : base("Validation.General", "Se han producido uno o más errores de validación.", ErrorType.Validation)
    {
        Errors = errors;
    }

    public static ValidationError FromErrors(Error[] errors) => new(errors);
}
