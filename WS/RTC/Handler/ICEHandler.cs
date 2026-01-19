using System.Net.WebSockets;
using System.Text.Json;
using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC.Handler;

public class ICEHandler : IMessageHandler
{
    private readonly RTCConnectionManager _connectionManager;
    private readonly ILogger<ICEHandler> _logger;

    public ICEHandler(RTCConnectionManager connectionManager, ILogger<ICEHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    public string MessageType => "Ice";
    public async Task<WebSocketMessage?> HandleAsync(WebSocketMessage message, string roomId, string? clientId)
    {
        try
        {
            WebSocket? to;
            string sender = string.Empty;
            if (String.IsNullOrWhiteSpace(clientId))
            {
                // clientId 가 없을경우 broadcaster 쪽 
                if (String.IsNullOrWhiteSpace(message.SenderId))
                {
                    _logger.LogError("[ICEHandler] SenderId가 없습니다.");
                    return CreateMessage(false, roomId, "SenderId가 필요합니다.");
                }
                
                var client = _connectionManager.GetClient(message.SenderId);
                
                if (client == null)
                {
                    _logger.LogError($"[ICEHandler] 클라이언트를 찾을 수 없습니다: {message.SenderId}");
                    return CreateMessage(false, roomId, $"클라이언트를 찾을 수 없습니다: {message.SenderId}");
                }
            
                to = client.Socket;
                sender = roomId;
            }
            else
            {
                to = _connectionManager.GetBroadcasterByRoomId(roomId);
                sender = clientId;
            }

            if (to == null)
            {
                _logger.LogError("[ICEHandler] ice전송 대상이 없습니다.");
                return CreateMessage(false, roomId, "ice전송 대상이 없습니다.");
            }

            await SendICEAsync(sender, to, message.Payload);

            return CreateMessage(true, roomId, $"[ICEHandler] {sender} 에서 ice 전송");
        }
        catch (Exception ex)
        {
            _logger.LogError("[ICEHandler] ice전송 중 오류 발생");
            return CreateMessage(false, roomId, ex.Message);
        }
    }

    public async Task SendICEAsync(string sender, WebSocket to, string message)
    {
        var msg = new WebSocketMessage
        {
            Type = "ice",
            Payload = message,
            SenderId = sender,
            Timestamp = DateTimeOffset.Now
        };
        
        var bytes =  JsonSerializer.SerializeToUtf8Bytes(msg);

        if (to.State == WebSocketState.Open)
        {
            await to.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        else
        {
            throw new Exception("전송 대상 소켓이 닫혀있습니다.");
        }
    }
    
    // Ice 처리에 대한 리턴 값 
    public WebSocketMessage CreateMessage(bool success, string roomId, string message)
    {
        var response = new ResponseMessage
        {
            Success = success,
            RoomId = roomId,
            Message = message,
            Timestamp = DateTimeOffset.Now
        };

        return new WebSocketMessage
        {
            Type = "System",
            Payload = JsonSerializer.Serialize(response),
            Timestamp = DateTimeOffset.Now
        };
    }
}