using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R.O.S.C.H.WS.Opc.DTO;

namespace R.O.S.C.H.WS.Opc;

public class OpcWebSocketManager
{
    private static readonly ConcurrentDictionary<string, OpcClientDto> _clients = new();
    private static readonly ConcurrentDictionary<string, OpcClientDto> _unity = new();
    private readonly ILogger<OpcWebSocketManager> _logger;
    
    public OpcWebSocketManager(ILogger<OpcWebSocketManager> logger)
    {
        _logger = logger;
    }

    public void RegisterClient(string clientId, OpcClientDto client, bool isUnity)
    {
        if (isUnity)
        {
            _unity.TryAdd(clientId, client);
        }
        else
        {
            _clients.TryAdd(clientId, client);
        }
    }

    public void UnregisterClient(string clientId, bool IsUnity)
    {
        if (IsUnity)
        {
            _unity.TryRemove(clientId, out _);
        }
        else
        {
            _clients.TryRemove(clientId, out _);
        }
    }

    public async Task SendDataAsync(IDictionary<string, object> sendData)
    {
        JObject obj = JObject.FromObject(sendData);
        // client에는 컨베이어 속도만 전송
        
        JObject sendToClient = new JObject();
        sendToClient.Add("conv_load", obj["stm_stm_yolo_currentspeedload"].ToString());
        sendToClient.Add("conv_main", obj["stm_stm_yolo_currentspeedmain"].ToString());
        sendToClient.Add("conv_sort", obj["stm_stm_yolo_currentspeedsort"].ToString());

        foreach (var client in _clients)
        {
            try
            {
                var sock = client.Value.Socket;

                if (sock.State != WebSocketState.Open)
                {
                    _clients.TryRemove(client.Key, out _);
                    continue;
                }

                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sendToClient));
                await client.Value.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError($"[OPCSocketMiddleware] client {client.Key} 에게 데이터 전송 중 오류 : " + e.Message);
            }
        }
        
        // unity에는 전체 데이터 전송
        foreach (var client in _unity)
        {
            try
            {
                var sock = client.Value.Socket;

                if (sock.State != WebSocketState.Open)
                {
                    _unity.TryRemove(client.Key, out _);
                    continue;
                }

                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
                await client.Value.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            } catch (Exception e)
            {
                _logger.LogError($"[OPCSocketMiddleware] unity {client.Key} 에게 데이터 전송 중 오류 : " + e.Message);
            }
        }
    }
}