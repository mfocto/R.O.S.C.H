using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R.O.S.C.H.WS.Common;
using R.O.S.C.H.WS.Models;
using R.O.S.C.H.WS.RTC.DTO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace R.O.S.C.H.WS.RTC;

public class RTCConnectionManager(ILogger<RTCConnectionManager> logger)
{
    private ILogger _logger = logger;
    
    
    public readonly ConcurrentDictionary<string, WebSocket> _BroadCasters = new();
    public readonly ConcurrentDictionary<string, ClientDTO> _Clients = new();
    
    // 연결처리 
    public void AddBroadcaster(WebSocket socket, string roomId)
    {
        _BroadCasters.TryAdd(roomId, socket);
        
        _logger.LogInformation($"[RTCConnectionManager] {roomId} 브로드캐스터 등록 완료");
        SendMessageAsync(socket, $"{roomId} - Broadcaster 등록되었습니다");
    }
    
    // 연결해제
    public void RemoveBroadcaster(WebSocket socket, string roomId)
    {
        _BroadCasters.TryRemove(roomId, out _);
               
        _logger.LogInformation($"[RTCConnectionManager] {roomId} 브로드캐스터 연결 해제");
        SendMessageAsync(socket, $"{roomId} - Broadcaster 연결 해제되었습니다.");
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
            socket =  socket,
            RoomId = roomId,
            status = "JOINED",
            ConnectedAt = DateTimeOffset.UtcNow
        };
        
        _Clients.TryAdd(clientId, client);
        
        _logger.LogInformation($"[RTCConnectionManager] {clientId} 클라이언트 등록 완료");
        return clientId;
    }

    public void ChangeRoom(string clientId, string roomId)
    {
        if (_Clients.TryGetValue(clientId, out var client))
        {
            client.RoomId = roomId;
            client.LastUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private async void SendMessageAsync(WebSocket socket, string message)
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
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    
}

