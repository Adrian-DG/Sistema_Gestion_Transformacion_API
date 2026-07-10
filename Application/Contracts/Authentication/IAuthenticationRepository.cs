using Domain.Common;

namespace Application.Contracts.Authentication
{
    public interface IAuthenticationRepository
    {
        Task<Result<string>> Login(string username, string password);
    }
}
