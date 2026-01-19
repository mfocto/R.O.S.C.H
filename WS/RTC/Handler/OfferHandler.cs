using System.Net.WebSockets;
using System.Text.Json;
using R.O.S.C.H.WS.Common;


namespace R.O.S.C.H.WS.RTC.Handler;

public class OfferHandler : IMessageHandler
{
    private readonly RTCConnectionManager _connectionManager;
    private readonly ILogger<OfferHandler> _logger;
    public string MessageType => "Offer";

    public OfferHandler(RTCConnectionManager connectionManager, ILogger<OfferHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }
    public async Task<WebSocketMessage?> HandleAsync(WebSocketMessage message, string roomId, string? clientId)
    {
        _logger.LogInformation($"[OfferHandler] {clientId} Offer");
        try
        {
            // roomId로 브로드캐스터 등록되어있는지 확인
            var broadcaster = _connectionManager.GetBroadcasterByRoomId(roomId);

            if (broadcaster == null)
            {
                _logger.LogError("[OfferHandler] broadcaster is null");
                return CreateMessage(false, roomId, "해당 브로드캐스터가 등록되어있지 않습니다.");
            }
            
            if (String.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("[OfferHandler] clientId is null");
                return CreateMessage(false, roomId, "클라이언트 아이디가 없습니다.");
            }
            
            // 전송처리 
            await SendOfferMessage(broadcaster, clientId, message.Payload);
            
            // 성공 시 client 상태 값 변경
            _connectionManager.UpdateClient(clientId, roomId, "OFFER_SEND");
            
            return  CreateMessage(true, roomId, "OFFER_SEND");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[OfferHandler] 연결 처리 중 오류 발생!");
            return CreateMessage(false, roomId, ex.Message);
        }
    }

    public async Task SendOfferMessage(WebSocket socket, string clientId, string message)
    {
        var offer = new WebSocketMessage
        {
            Type = "offer",
            Payload = message,
            SenderId = clientId,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(offer);

        if (socket.State == WebSocketState.Open)
        {
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        else
        {
            throw new Exception("전송 대상 소켓이 닫혀있습니다.");
        }
    }
    
    // offer 처리에 대한 리턴 값 
    public WebSocketMessage CreateMessage(bool success, string roomId, string message)
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