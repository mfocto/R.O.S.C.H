

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
        while (true)
        {
            var opcExt = await _opc.ReadStateAsync(ct);
            _logger.LogInformation("읽는중");
        }
    }
}