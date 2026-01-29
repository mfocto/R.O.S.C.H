using R.O.S.C.H.API.DTO;
using R.O.S.C.H.API.Service.Interface;

namespace R.O.S.C.H.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi();

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithOpenApi();
    }

    private static async Task<IResult> Login(
        AuthRequest request, 
        IAuthService authService)
    {
        var result = await authService.AuthenticateAsync(request);

        if (!result.Success)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }

    private static IResult Logout()
    {
        // 로그아웃 로직 (세션 무효화, 토큰 제거 등)
        return Results.Ok(new { success = true, message = "로그아웃되었습니다." });
    }
}