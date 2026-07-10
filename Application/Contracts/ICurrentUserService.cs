namespace Application.Contracts
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
    }
}
