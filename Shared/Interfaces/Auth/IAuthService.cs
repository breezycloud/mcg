using Shared.Models.Auth;

namespace Shared.Interfaces.Auth;

public interface IAuthService
{
    Task<LoginResponse?> Login(LoginModel login, CancellationToken cancellationToken);
}