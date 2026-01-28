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
    public async Task<WebSocketMessage?> HandleAsync(WebSocketMessage message)
    {
        string roomId = string.Empty;
        try
        {
            _logger.LogDebug("[ICEHandler] sender: {message}, payload: {}", message.SenderId, message.Payload);
            if (message.SenderType.Equals("Broadcaster"))
            {
                // Broadcaster 면 receiverId 기준으로 Client 객체 찾아오기
                roomId = message.SenderId;
                var client = _connectionManager.GetClient(message.ReceiverId);

                if (client == null)
                {
                    _logger.LogError($"[ICEHandler] {message.ReceiverId}에 해당하는 Client를 찾을 수 없습니다.");
                    return null;
                }

                await SendICEAsync(message.SenderId, message.ReceiverId, client.Socket, message.Payload);
            }
            else
            {
                roomId = message.ReceiverId;
                var broadcaster = _connectionManager.GetBroadcasterByRoomId(roomId);

                if (broadcaster == null)
                {
                    _logger.LogError($"[ICEHandler] {message.ReceiverId}에 해당하는 Broadcaster 찾을 수 없습니다.");
                    return null;
                }
                
                await SendICEAsync(message.SenderId, message.ReceiverId, broadcaster, message.Payload);
            }
            
            // ice는 각 전송마다 메시지 리턴하면 너무 많이 전송함
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ICEHandler] ice전송 중 오류 발생");
            return CreateMessage(false, 
                roomId,
                ex.Message);
        }
    }

    public async Task SendICEAsync(string sender, string receiver, WebSocket to, string message)
    {
        var msg = new WebSocketMessage
        {
            Type = "ice",
            Payload = message,
            SenderId = sender,
            ReceiverId = receiver,
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