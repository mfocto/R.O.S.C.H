using System.Collections.Concurrent;
using System.Net.WebSockets;
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

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest) await _next(context);
        
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var clientType = context.Request.Query["type"].ToString() ?? "unknown";
        
        OpcClientDto client =  new OpcClientDto(clientType, socket);
        
        // dto 생성시 만들어지는 clientId로 client 등록
        _clients.TryAdd(client.ClientId, client);
        _logger.LogDebug("OPC client connected");
        
        
    }
}