using R.O.S.C.H.API.DTO;

namespace R.O.S.C.H.API.Service.Interface;

public interface IAuthService
{
    Task<AuthResponse> AuthenticateAsync(AuthRequest request); 
    Task<AuthResponse> LogoutAsync(AuthRequest request);
}