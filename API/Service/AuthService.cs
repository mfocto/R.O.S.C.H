using Npgsql;
using R.O.S.C.H.API.DTO;
using R.O.S.C.H.API.Service.Interface;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.API.Service;

public class AuthService(
    IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IErrorLogRepository errorLogRepository
    ) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly IErrorLogRepository _errorLogRepository = errorLogRepository;


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
            
            // 에러 로그 DB에 저장
            await SaveErrorLogToDbAsync("AuthService.AuthenticateAsync", e.Message, e.StackTrace);
            
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
            
            // 에러 로그 DB에 저장
            await SaveErrorLogToDbAsync("AuthService.LogoutAsync", e.Message, e.StackTrace);
            
            return new AuthResponse
            {
                Success = false,
                Message = "로그아웃 처리 중 오류가 발생했습니다."
            };
        }
    }
    
    private async Task SaveErrorLogToDbAsync(string errorSource, string errorMsg, string? stackTrace, int? deviceId = null, int? userId = null)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            
            using var tx = await conn.BeginTransactionAsync();
            
            var errorLog = new ErrorLog
            {
                ErrorCode = "E004", // 시스템 오류
                ErrorSource = errorSource,
                ErrorMsg = errorMsg,
                StackTrace = stackTrace,
                DeviceId = deviceId ?? 0,
                UserId = userId ?? 0
            };
            
            await _errorLogRepository.CreateErrorLog(conn, tx, errorLog);
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthService] 에러 로그 DB 저장 실패");
        }
    }
}