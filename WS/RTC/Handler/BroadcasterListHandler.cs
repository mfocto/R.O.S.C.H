using System.Net.WebSockets;
using System.Text.Json;
using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC.Handler;

/// <summary>
/// client가 연결할 수 있는 Broadcaster List 제공
/// </summary>
public class BroadcasterListHandler: IMessageHandler
{
    private readonly RTCConnectionManager _connectionManager;
    private readonly ILogger<BroadcasterListHandler> _logger;

    public BroadcasterListHandler(RTCConnectionManager connectionManager, ILogger<BroadcasterListHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    public string MessageType => "BroadcasterList";
    public async Task<WebSocketMessage?> HandleAsync(WebSocketMessage message)
    {
        try
        {
            var clientId = message.SenderId;
            if (string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("[BroadcasterListHandler] clientId 값이 없습니다.");
                return CreateMessage(false, "clientId 값이 없습니다.");
            }
            
            // 현재 등록되어있는 broadcaster 전체 조회
            var broadcasters = _connectionManager.GetBroadcasters();
            if (broadcasters.Count == 0)
            {
                _logger.LogWarning("[BroadcasterListHandler] 현재 등록되어있는 브로드캐스터가 없습니다.");
                return CreateMessage(false, "현재 등록되어있는 브로드캐스터가 없습니다.");
            }

            var client = _connectionManager.GetClient(clientId);

            if (client == null)
            {
                _logger.LogError($"[BroadcasterListHandler] {clientId}로 조회된 객체가 없습니다.");
                return CreateMessage(false, "등록되지 않은 client입니다.");
            }

            await SendBroadcasterListAsync(client.Socket, broadcasters);

            return CreateMessage(true, "브로드캐스터 리스트 조회 완료");
        }   
        catch (Exception ex)
        {
            _logger.LogError("[BroadcasterListHandler] 브로드캐스터 리스트 조회 중 오류 : " + ex.Message);
            return CreateMessage(false, "브로드캐스터 리스트 조회 중 오류가 발생하였습니다.");
        }
    }

    public async Task SendBroadcasterListAsync(WebSocket socket, ICollection<string> broadcasters)
    {
        if (socket.State != WebSocketState.Open)
        {
            _logger.LogError("[BroadcasterListHandler] 전송 대상 소켓이 닫혀있습니다.");
            throw new Exception("전송 대상 소켓이 닫혀있습니다.");
        } 
        
        string jsonString = JsonSerializer.Serialize(broadcasters);

        WebSocketMessage message = new WebSocketMessage
        {
            Type = "broadcasterList",
            Payload = jsonString,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
        
        await socket.SendAsync(new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

    }

    public WebSocketMessage CreateMessage(bool success, string message)
    {
        var response = new ResponseMessage
        {
            Success = success,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        };

        return new WebSocketMessage
        {
            Type = "System",
            Payload = JsonSerializer.Serialize(response),
            Timestamp =  DateTimeOffset.UtcNow
        };
    }
}