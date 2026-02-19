using R.O.S.C.H.WS.Opc;
using R.O.S.C.H.WS.RTC;

namespace R.O.S.C.H;

public class ShutdownHostedService : IHostedService
{
    
    private readonly ILogger<ShutdownHostedService> _logger;
    private readonly RTCConnectionManager  _rtcConnectionManager;
    private readonly OpcWebSocketManager _opcWebSocketManager;

    public ShutdownHostedService(ILogger<ShutdownHostedService> logger, RTCConnectionManager rtcConnectionManager,
        OpcWebSocketManager opcWebSocketManager)
    {
        _logger = logger;
        _rtcConnectionManager = rtcConnectionManager;
        _opcWebSocketManager = opcWebSocketManager;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutdown Service 시작");

        try
        {
            await Task.WhenAll(
                _rtcConnectionManager.CloseAllConnectionAsync(cancellationToken),
                _opcWebSocketManager.CloseAllConnectionAsync(cancellationToken)
            );
        }
        catch (OperationCanceledException e)
        {
            /**/
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
        
        _logger.LogInformation("Shutdown Service 완료");
    } 
}