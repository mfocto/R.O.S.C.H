

using Npgsql;
using R.O.S.C.H.adapter;
using R.O.S.C.H.adapter.Interface;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;
using R.O.S.C.H.WS.Opc;

namespace R.O.S.C.H.Worker;

public class StatePollingWorker: BackgroundService
{
    private IConfiguration _config;
    private ILogger<StatePollingWorker> _logger;
    private IOpcUaAdapter _opc;
    private readonly OpcWebSocketManager _manager;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public StatePollingWorker(
        IConfiguration config,
        ILogger<StatePollingWorker> logger,
        IOpcUaAdapter opc,
        OpcWebSocketManager manager,
        IServiceScopeFactory serviceScopeFactory
        )
    {
        _config = config;
        _logger = logger;
        _opc = opc;
        _manager = manager;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 데이터 수집
                IDictionary<string, object> opcExt = await _opc.ReadStateAsync(ct);
                if (_logger.IsEnabled(LogLevel.Debug))
                {   
                    // 로그 레벨이 Debug 면 값 확인
                    foreach (var opc in opcExt)
                    {
                        _logger.LogDebug("[StatePollingWorker] 값 확인 => " + opc.Key + " : " + opc.Value);
                    }
                }
                
                // DB에 OPC 데이터 저장
                await SaveOpcDataToDbAsync(opcExt);
                
                // 수집한 데이터 unity로 전송
                await _manager.SendDataAsync(opcExt);
            }
            catch (Exception ex)
            {
                // 오류나도 프로그램 종료되지 않도록 로그만 추가
                _logger.LogError(ex, ex.Message);
                
                // 에러 로그 DB에 저장 
                try
                {
                    await SaveErrorLogToDbAsync("StatePollingWorker.ExecuteAsync", ex.Message, ex.StackTrace);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "[StatePollingWorker] 에러 로그 저장 실패 (무시됨)");
                }
            }
            // 1초마다 polling
            await Task.Delay(1000, ct);
        }
    }
    
    private async Task SaveOpcDataToDbAsync(IDictionary<string, object> opcData)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var opcDataRepo = scope.ServiceProvider.GetRequiredService<IOpcDataRepository>();
            var deviceRepo = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            var connectionString = _config.GetConnectionString("DefaultConnection");
            
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            
            using var tx = await conn.BeginTransactionAsync();
            
            var sourceTime = DateTimeOffset.UtcNow;
            
            foreach (var item in opcData)
            {
                try
                {
                    // key 형식: "channel_device_tag" (예: "stm_stm_yolo_currentspeedload")
                    var parts = item.Key.Split('_');
                    if (parts.Length < 3) continue;
                    
                    string channel = parts[0];
                    string deviceName = string.Join("_", parts.Skip(1).Take(parts.Length - 2));
                    string tag = parts[^1];
                    
                    // device_id 조회 - device_alias로 매핑 필요
                    // deviceName이 "stm_yolo"인 경우 -> 여러 device와 연관됨
                    // 간단하게 device_id를 1로 설정하거나, 매핑 로직 추가 필요
                    // 여기서는 임시로 deviceId = 1 사용
                    int deviceId = 1; // TODO: device 매핑 로직 필요 시 개선
                    
                    var opcDataModel = new OpcData
                    {
                        DeviceId = deviceId,
                        TagName = item.Key,
                        TagValue = item.Value?.ToString() ?? "",
                        DataType = item.Value?.GetType().Name ?? "Unknown",
                        SourceTime = sourceTime
                    };
                    
                    await opcDataRepo.CreateOpcData(conn, tx, opcDataModel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"[StatePollingWorker] OPC 데이터 저장 실패: {item.Key}");
                }
            }
            
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StatePollingWorker] OPC 데이터 DB 저장 중 오류");
            await SaveErrorLogToDbAsync("StatePollingWorker.SaveOpcDataToDbAsync", ex.Message, ex.StackTrace);
        }
    }
    
    private async Task SaveErrorLogToDbAsync(string errorSource, string errorMsg, string? stackTrace, int? deviceId = null)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var errorLogRepo = scope.ServiceProvider.GetRequiredService<IErrorLogRepository>();
            var connectionString = _config.GetConnectionString("DefaultConnection");
            
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            
            using var tx = await conn.BeginTransactionAsync();
            
            var errorLog = new ErrorLog
            {
                ErrorCode = "E001", // OPC UA 통신 오류
                ErrorSource = errorSource,
                ErrorMsg = errorMsg,
                StackTrace = stackTrace,
                DeviceId = deviceId ?? 0,
                UserId = 0
            };
            
            await errorLogRepo.CreateErrorLog(conn, tx, errorLog);
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StatePollingWorker] 에러 로그 DB 저장 실패");
        }
    }
}