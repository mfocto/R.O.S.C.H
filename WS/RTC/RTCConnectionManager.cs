using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;
using R.O.S.C.H.WS.Common;
using R.O.S.C.H.WS.RTC.DTO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace R.O.S.C.H.WS.RTC;

public class RTCConnectionManager(ILogger<RTCConnectionManager> logger)
{
    private ILogger _logger = logger;
    
    // key- roomId
    public readonly ConcurrentDictionary<string, WebSocket> _BroadCasters = new();
    // key - clientId
    public readonly ConcurrentDictionary<string, ClientDTO> _Clients = new();
    
    // 연결처리 
    public async Task AddBroadcaster(WebSocket socket, string roomId)
    {
        if (_BroadCasters.ContainsKey(roomId))
        {
            // 중복 등록 처리 방지
            if (_BroadCasters.TryRemove(roomId, out var oldSocket))
            {
                try
                {
                    if (oldSocket.State == WebSocketState.Open)
                    {
                        await oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                            "새로운 브로드캐스터가 등록되어 연결이 종료됩니다.",
                            CancellationToken.None);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"[RTCConnectionManager] 기존 브로드캐스터 소켓 종료 실패: {roomId}");
                }
                finally
                {
                    oldSocket.Dispose();
                }
            }
        }
        
        _BroadCasters.TryAdd(roomId, socket);
        
        _logger.LogInformation($"[RTCConnectionManager] {roomId} 브로드캐스터 등록 완료");
        await SendMessageAsync(socket, $"{roomId} - Broadcaster 등록되었습니다");
    }
    
    // 연결해제
    public async Task RemoveBroadcaster(WebSocket socket, string roomId)
    {
        _BroadCasters.TryRemove(roomId, out _);
               
        _logger.LogInformation($"[RTCConnectionManager] {roomId} 브로드캐스터 연결 해제");
        await SendMessageAsync(socket, $"{roomId} - Broadcaster 연결 해제되었습니다.");
    }

    public WebSocket? GetBroadcasterByRoomId(string roomId)
    {
        if (_BroadCasters.TryGetValue(roomId, out var socket))
        {
            return socket;
        }

        return null;
    }
 
    public ICollection<string> GetBroadcasters()
    {
        return _BroadCasters.Keys;
    }

    public string RegisterClient(WebSocket socket, string roomId)
    {
        var clientId =  Guid.NewGuid().ToString();

        var client = new ClientDTO
        {
            ClientId = clientId,
            Socket =  socket,
            RoomId = roomId,
            Status = "JOINED",
            JoinedAt = DateTimeOffset.UtcNow
        };
        
        _Clients.TryAdd(clientId, client);
        
        _logger.LogInformation($"[RTCConnectionManager] {clientId} 클라이언트 등록 완료");
        return clientId;
    }
    
    public void RemoveClient(string clientId)
    {
        _Clients.TryRemove(clientId, out _);
    }

    public void UpdateClient(string? clientId, string roomId, string status)
    {
        if (clientId == null) return;
        if (_Clients.TryGetValue(clientId, out var oldClient))
        {
            var updatedClient = new ClientDTO
            {
                ClientId = oldClient.ClientId,
                Socket = oldClient.Socket,
                RoomId = roomId,
                Status = status,
                JoinedAt = oldClient.JoinedAt,
                ConnectedAt = status.Equals("CONNECTED")?  DateTimeOffset.UtcNow 
                    : (status.Equals("DISCONNECTED")) ? null : oldClient.ConnectedAt,
                LastUpdatedAt =  DateTimeOffset.UtcNow
            };
            
            _Clients.TryUpdate(clientId, updatedClient, oldClient);
        }
    }

    public ClientDTO? GetClient(string clientId)
    {
        _Clients.TryGetValue(clientId, out var client);

        return client;
    }

    private async Task SendMessageAsync(WebSocket socket, string message)
    {
        var messageJson = new JObject();
        
        messageJson.Add("message", message);
        
        var response = new WebSocketMessage
        {
            Type = "System",
            Payload = messageJson.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

        if (socket.State == WebSocketState.Open)
        {
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
    }
    
}

