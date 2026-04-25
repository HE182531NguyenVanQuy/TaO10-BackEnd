using TaO10_BackEnd.DTOs.Auth;

namespace TaO10_BackEnd.Interfaces
{
    public interface IAuthService
    {
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
        Task<TokenResponse> LoginAsync(LoginRequest request);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);

    }
}
