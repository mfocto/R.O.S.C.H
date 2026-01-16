using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC;

public class HandshakeHandler
{
    private readonly ILogger<HandshakeHandler> _logger;

    public string MessageType => "handshake";

    public  HandshakeHandler(ILogger<HandshakeHandler> logger)
    {
        _logger = logger;
    }
    
    
}