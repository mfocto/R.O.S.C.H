using System.Net.WebSockets;
using System.Text.Json;
using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC.Handler;

public class AnswerHandler: IMessageHandler
{
    private RTCConnectionManager _connectionManager;
    private ILogger<AnswerHandler> _logger;

    public AnswerHandler(RTCConnectionManager connectionManager, ILogger<AnswerHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    public string MessageType => "Answer";

    public async Task<WebSocketMessage?> HandleAsync(WebSocketMessage message)
    {
        var roomId = message.SenderId;
        _logger.LogInformation($"[AnswerHandler] {roomId} Answer");
        try
        {
            if (String.IsNullOrWhiteSpace(message.SenderId))
            {
                _logger.LogError($"[AnswerHandler] senderId is null or empty");
                return CreateMessage(false, roomId, $"senderId가 없습니다.");
            }
            
            // answer 전송
            await SendAnswerAsync(roomId, message.SenderId, message.Payload);
            
            _connectionManager.UpdateClient(message.SenderId, roomId, "ANSWER_RECEIVED");
            
            return CreateMessage(true, roomId, "Answer 전송 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AnswerHandler] {ex.Message}");
            return CreateMessage(false, roomId, "Answer 전송 중 오류 발생");
        }
    }

    public async Task SendAnswerAsync(string roomId, string clientId, string message)
    {
        var answer = new WebSocketMessage
        {
            Type = "answer",
            Payload = message,
            SenderId = roomId,
            Timestamp = DateTimeOffset.UtcNow
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(answer);
        var client = _connectionManager.GetClient(clientId);

        if (client == null)
        {
            throw new Exception($"[AnswerHandler] client 정보가 없습니다.");
        }

        var socket = client.Socket;

        if (socket.State == WebSocketState.Open)
        {
            await client.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        else
        {
            throw new Exception("전송 대상 소켓이 닫혀있습니다.");
        }
    }
    
    // answer 처리에 대한 리턴 값 
    private WebSocketMessage CreateMessage(bool success, string roomId, string message)
    {
        var response = new ResponseMessage
        {
            Success = success,
            RoomId = roomId,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        return new WebSocketMessage
        {
            Type = "System",
            Payload = JsonSerializer.Serialize(response),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}