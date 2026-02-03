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

    public ControlService(
        IConfiguration configuration, 
        ILogger<ControlService> logger,  
        IDeviceRepository deviceRepository, 
        IUserRepository userRepository,
        IControlLogRepository controlLogRepository
        )
    {
        _configuration = configuration;
        _logger = logger;
        _deviceRepository = deviceRepository;
        _userRepository = userRepository;
        _controlLogRepository = controlLogRepository;
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

            if (user != null)
            {
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
            }
        }
        catch (Exception ex)
        {
            Log("error", "[ControlService.ControlLogProcess] 로그 DB처리 중 오류 :  " + ex.Message);
        }
    }

    public void Log(string type, string message)
    {
        if  (_logger.IsEnabled(LogLevel.Debug))
        {
            switch (type)
            {
                case "info" : _logger.LogInformation(message); break;
                case "warn" : _logger.LogWarning(message); break;
                case "error" : _logger.LogError(message); break;
                case "debug" :  _logger.LogDebug(message); break;
            }
        }
    }
}