using Npgsql;
using R.O.S.C.H.API.DTO;
using R.O.S.C.H.API.Service.Interface;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.API.Service;

public class AuthService(
    IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger
    ) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AuthService> _logger = logger;


    public async Task<AuthResponse> AuthenticateAsync(AuthRequest request)
    {
        try
        {
            // 들어온 값 검사
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "아이디와 비밀번호를 입력해주세요"
                };
            }
            
            // DB연결
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            
            // username으로 검색
            var user = await _userRepository.GetUserByUserNameAsync(conn, request.Username);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "아이디 또는 비밀번호가 일치하지 않습니다."
                };
            }
            
            // 비밀번호 검증
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "아이디 또는 비밀번호가 일치하지 않습니다."
                };
            }

            // ADMIN 은 하나만 로그인 가능하도록
            if (user.Role.Equals("ADMIN"))
            {
                if (user.IsActive)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "이미 접속한 계정입니다."
                    };
                }
                
                user.IsActive = true;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                
                using var tx = await conn.BeginTransactionAsync();
                await _userRepository.UpdateAsync(conn, tx,  user);
                await tx.CommitAsync();
            }
            
            return new AuthResponse
            {
                Success = true,
                Message = "",
                Role = user.Role,
            };
        }
        catch (Exception e)
        {
            _logger.LogError("[AuthService] 로그인 처리 중 오류 발생 : " + e.Message);
            return new AuthResponse
            {
                Success = false,
                Message = "로그인 처리 중 오류 발생했습니다."
            };
        }
    }

    public async Task<AuthResponse> LogoutAsync(AuthRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username))
            {
                throw new Exception("아이디가 없습니다.");
            }
            
            // 아이디로 객체 가져오기
            var  connectionString = _configuration.GetConnectionString("DefaultConnection");
            var conn = new NpgsqlConnection(connectionString);
            
            await conn.OpenAsync();
            
            var user = await _userRepository.GetUserByUserNameAsync(conn, request.Username);

            if (user.Role.Equals("ADMIN"))
            {
                // admin 이면 사용중 여부 false로 변경
                user.IsActive = false;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                
                using var tx =  conn.BeginTransaction();
                await _userRepository.UpdateAsync(conn, tx,  user);
                await tx.CommitAsync();
            }

            return new AuthResponse
            {
                Success = true,
            };
        }   
        catch (Exception e)
        {
            _logger.LogError("[AuthService] 로그아웃 처리 중 오류 발생 : " + e.Message);
            return new AuthResponse
            {
                Success = false,
                Message = "로그아웃 처리 중 오류가 발생했습니다."
            };
        }
    }
}