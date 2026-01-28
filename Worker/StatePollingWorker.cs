

using R.O.S.C.H.adapter;

namespace R.O.S.C.H.Worker;

public class StatePollingWorker: BackgroundService
{
    private IConfiguration _config;
    private ILogger<StatePollingWorker> _logger;
    private OpcUaAdapter _opc;

    public StatePollingWorker(
        IConfiguration config,
        ILogger<StatePollingWorker> logger,
        OpcUaAdapter opc
        )
    {
        _config = config;
        _logger = logger;
        _opc = opc;
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
                
                // 수집한 데이터 unity로 전송

            }
            catch (Exception ex)
            {
                // 오류나도 프로그램 종료되지 않도록 로그만 추가
                _logger.LogError(ex, ex.Message);
            }
            
            
            
            await Task.Delay(5000, ct);
        }
    }
}