using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC.Handler;

public class CameraChangeHandler : IMessageHandler
{
    
    private readonly RTCConnectionManager _connectionManager;
    private readonly ILogger<CameraChangeHandler> _logger;
    public string MessageType => "cameraChange";
    
    public CameraChangeHandler(
        RTCConnectionManager connectionManager,
        ILogger<CameraChangeHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    public async Task<WebSocketMessage?> HandleAsync(WebSocketMessage message)
    {
        try
        {
            var cameraName = message.Payload;
            var roomId = message.ReceiverId;

            _logger.LogInformation($"[CameraChange] Client {message.SenderId} → Camera: {cameraName}");

            // Broadcaster에게 카메라 변경 메시지 전송
            WebSocket socket = _connectionManager.GetBroadcasterByRoomId(roomId);
            await SendToBroadcasterAsync(socket, message, roomId);

            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CameraChange] 카메라 변경 처리 중 오류");
        }
        
        return new WebSocketMessage();
    }

    public async Task SendToBroadcasterAsync(WebSocket socket, WebSocketMessage message, string roomId)
    {
        if (socket.State != WebSocketState.Open)
        {
            _logger.LogError("[BroadcasterListHandler] 전송 대상 소켓이 닫혀있습니다.");
            throw new Exception("전송 대상 소켓이 닫혀있습니다.");
        } 
        
        var socketMessage =  new WebSocketMessage
        {
            Type = "cameraChange",
            SenderId =  message.SenderId,
            ReceiverId =  message.ReceiverId,
            Payload = message.Payload,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(socketMessage));
        
        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}