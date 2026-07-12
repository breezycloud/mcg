using Shared.Models.Auth;

namespace Shared.Interfaces.Auth;

public interface IAuthService
{
    Task<LoginResponse?> Login(LoginModel login, CancellationToken cancellationToken);
    Task<bool> ForgotPassword(ForgotPasswordModel model, CancellationToken cancellationToken);
    Task<bool> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken);
    Task<LoginResponse?> ChangePassword(ChangePasswordModel model, CancellationToken cancellationToken);
    Task<LoginResponse?> RefreshToken(string refreshToken, CancellationToken cancellationToken);
    Task Logout(string refreshToken, CancellationToken cancellationToken);
}