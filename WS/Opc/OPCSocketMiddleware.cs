using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R.O.S.C.H.WS.Opc.DTO;

namespace R.O.S.C.H.WS.Opc;

public class OPCSocketMiddleware(
    ILogger<OPCSocketMiddleware> logger,
    RequestDelegate next,
    OpcWebSocketManager manager
    )
{
    private readonly ILogger<OPCSocketMiddleware> _logger = logger;
    private readonly RequestDelegate _next = next;
    private readonly OpcWebSocketManager _manager = manager;
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest) await _next(context);
        
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var clientType = context.Request.Query["type"].ToString() ?? "unknown";
        
        OpcClientDto client =  new OpcClientDto(clientType, socket);
        
        _manager.RegisterClient(client.ClientId, client, clientType.Equals("unity"));
        
        var buffer = new byte[1024 * 8];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"WebSocket error for client {client.ClientId}");
        }
        finally
        {
            _manager.UnregisterClient(client.ClientId, clientType.Equals("unity"));
        }
    }

   
}