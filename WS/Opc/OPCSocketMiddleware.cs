using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R.O.S.C.H.WS.Opc.DTO;

namespace R.O.S.C.H.WS.Opc;

public class OPCSocketMiddleware(
    ILogger<OPCSocketMiddleware> logger,
    RequestDelegate next
    )
{
    private readonly ILogger<OPCSocketMiddleware> _logger = logger;
    private readonly RequestDelegate _next = next;
    private static readonly ConcurrentDictionary<string, OpcClientDto> _clients = new ConcurrentDictionary<string, OpcClientDto>();
    private static readonly ConcurrentDictionary<string, OpcClientDto> _unity = new ConcurrentDictionary<string, OpcClientDto>();
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest) await _next(context);
        
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var clientType = context.Request.Query["type"].ToString() ?? "unknown";
        
        OpcClientDto client =  new OpcClientDto(clientType, socket);
        
        // dto 생성시 만들어지는 clientId로 client 등록
        if (clientType.Equals("unity"))
        {
            _unity.TryAdd(client.ClientId, client);
        }
        else
        {
            _clients.TryAdd(client.ClientId, client);
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
                    _clients.TryRemove(client.Key, out _);
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