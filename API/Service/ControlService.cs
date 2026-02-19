using Npgsql;
using R.O.S.C.H.API.DTO;
using R.O.S.C.H.API.Service.Interface;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.API.Service;

public class ControlService: IControlService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ControlService> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IControlLogRepository _controlLogRepository;
    private readonly IErrorLogRepository _errorLogRepository;

    public ControlService(
        IConfiguration configuration, 
        ILogger<ControlService> logger,  
        IDeviceRepository deviceRepository, 
        IUserRepository userRepository,
        IControlLogRepository controlLogRepository,
        IErrorLogRepository errorLogRepository
        )
    {
        _configuration = configuration;
        _logger = logger;
        _deviceRepository = deviceRepository;
        _userRepository = userRepository;
        _controlLogRepository = controlLogRepository;
        _errorLogRepository = errorLogRepository;
    }


    public async Task ControlLogProcess(ControlRequest request, string deviceAlias = "")
    {
        try
        {
            Log("info", "[ControlService.ControlLogProcess] db 처리 시작");
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var conn = new NpgsqlConnection(connectionString);

            await conn.OpenAsync();
            Device? device = null;
            // 기기명으로 기기 검색 
            if (!string.IsNullOrEmpty(deviceAlias))
            {
                device = await _deviceRepository.GetDevice(conn, deviceAlias);
            }
            
            var user = await _userRepository.GetUserByUserNameAsync(conn, request.UserName);

            ControlLog controlLog;
            if (device != null)
            {
                controlLog = new ControlLog
                {
                    UserId = user.UserId,
                    DeviceId = device.DeviceId,
                    NewValue = Convert.ToString(request.Value)!,
                    TagName = "Stm_yolo.TargetState",
                    ControlType = Convert.ToString(request.Value)!.ToUpper()
                };
            }
            else
            {
                controlLog = new ControlLog
                {
                    UserId = user.UserId,
                    DeviceId = 0,
                    NewValue = Convert.ToString(request.Value)!,
                    TagName = "Process",
                    ControlType = Convert.ToString(request.Value)!.ToUpper()
                };
            }

            using var tx = await conn.BeginTransactionAsync();
            await _controlLogRepository.CreateAsync(conn, tx, controlLog);
            await tx.CommitAsync();
            
        }
        catch (Exception ex)
        {
            Log("error", "[ControlService.ControlLogProcess] 로그 DB처리 중 오류 :  " + ex.Message);
            
            // 에러 로그 DB에 저장
            await SaveErrorLogToDbAsync("ControlService.ControlLogProcess", ex.Message, ex.StackTrace);
        }
    }

    public void Log(string type, string message)
    {
        switch (type)
        {
            case "info" : _logger.LogInformation(message); break;
            case "warn" : _logger.LogWarning(message); break;
            case "error" : _logger.LogError(message); break;
            case "debug" :  _logger.LogDebug(message); break;
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
                ErrorCode = "E003", // 제어 오류
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
            _logger.LogError(ex, "[ControlService] 에러 로그 DB 저장 실패");
        }
    }
}