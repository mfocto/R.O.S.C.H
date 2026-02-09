using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;
using R.O.S.C.H.WS.Opc.DTO;

namespace R.O.S.C.H.WS.Opc;

public class OPCSocketMiddleware(
    ILogger<OPCSocketMiddleware> logger,
    RequestDelegate next,
    OpcWebSocketManager manager,
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration config
    )
{
    private readonly ILogger<OPCSocketMiddleware> _logger = logger;
    private readonly RequestDelegate _next = next;
    private readonly OpcWebSocketManager _manager = manager;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IConfiguration _config = config;
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
            
            // 에러 로그 DB에 저장 
            try
            {
                await SaveErrorLogToDbAsync("OPCSocketMiddleware.InvokeAsync", ex.Message, ex.StackTrace);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "[OPCSocketMiddleware] 에러 로그 저장 실패 (무시됨)");
            }
        }
        finally
        {
            _manager.UnregisterClient(client.ClientId, clientType.Equals("unity"));
        }
    }
    
    private async Task SaveErrorLogToDbAsync(string errorSource, string errorMsg, string? stackTrace, int? deviceId = null)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var errorLogRepo = scope.ServiceProvider.GetRequiredService<IErrorLogRepository>();
            var connectionString = _config.GetConnectionString("DefaultConnection");
            
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            
            using var tx = await conn.BeginTransactionAsync();
            
            var errorLog = new ErrorLog
            {
                ErrorCode = "E001", // OPC UA 통신 오류
                ErrorSource = errorSource,
                ErrorMsg = errorMsg,
                StackTrace = stackTrace,
                DeviceId = deviceId ?? 0,
                UserId = 0
            };
            
            await errorLogRepo.CreateErrorLog(conn, tx, errorLog);
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OPCSocketMiddleware] 에러 로그 DB 저장 실패");
        }
    }

   
}