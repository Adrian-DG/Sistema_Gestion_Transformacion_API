namespace Domain.Common;

public class Error : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    protected Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public bool Equals(Error? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Code == other.Code && Type == other.Type;
    }

    public override bool Equals(object? obj) => Equals(obj as Error);
    public override int GetHashCode() => HashCode.Combine(Code, Type);
    public static bool operator ==(Error? left, Error? right) => Equals(left, right);
    public static bool operator !=(Error? left, Error? right) => !Equals(left, right);
}
