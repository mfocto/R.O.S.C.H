using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC.Handler;

public class PingHandler(RTCConnectionManager connectionManager, ILogger<PingHandler> logger): IMessageHandler
{
    private readonly ILogger<PingHandler> _logger = logger;
    private RTCConnectionManager _connectionManager = connectionManager;
    public string MessageType => "Ping";
    public async Task<WebSocketMessage?> HandleAsync(WebSocketMessage message, string roomId, string? clientId)
    {
        try
        {
            
            if (clientId == null || string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("[PingHandler] clientId is null or empty");
                return null;
            }
            
            var clientDto = _connectionManager.GetClient(clientId);
            if (clientDto == null)
            {
                _logger.LogError("[PingHandler] clientInfo is null");
                return null;
            }
            
            var pongMessage = new WebSocketMessage
            {
                Type = "Pong",
                Payload = message.Payload,
                SenderId = clientId,
                Timestamp = DateTimeOffset.UtcNow
            };

            return pongMessage;
        }
        catch (Exception e)
        {
            _logger.LogError("[PingHandler] ping 처리 중 오류" + e.Message);
            return null;
        }
    }
}