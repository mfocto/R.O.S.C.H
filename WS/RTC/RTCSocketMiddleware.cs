using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;
using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC;

public class RTCSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RTCConnectionManager _connectionManager;
    private readonly ILogger<RTCSocketMiddleware> _logger;
    private readonly Dictionary<string, IMessageHandler> _handlers;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _config;
    
    
    
    public RTCSocketMiddleware(
        RequestDelegate next,
        RTCConnectionManager connectionManager,
        ILogger<RTCSocketMiddleware> logger,
        IEnumerable<IMessageHandler> handlers,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration config)
    {
        _next = next;
        _connectionManager = connectionManager;
        _logger = logger;
        _handlers = handlers.ToDictionary(h => h.MessageType,  h => h);
        _serviceScopeFactory = serviceScopeFactory;
        _config = config;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }
        
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    
        
        string clientId = string.Empty;
        string clientType = context.Request.Query["type"].ToString();
        string roomId = context.Request.Query["roomId"].ToString();
        
        #region 쿼리스트링 값 검증
        if (String.IsNullOrWhiteSpace(clientType))
        {
            _logger.LogWarning($"[RTCSocketMiddleware] clientType이 없습니다. IP: {context.Connection.RemoteIpAddress}");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("type은 필수입니다. (Broadcaster 또는 Client)");
            return;
        }
    
        if (!clientType.Equals("Broadcaster", StringComparison.OrdinalIgnoreCase) 
            && !clientType.Equals("Client", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning($"[RTCSocketMiddleware] 잘못된 clientType: {clientType}");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("type은 'Broadcaster' 또는 'Client'여야 합니다.");
            return;
        }
        #endregion

        try
        {
            clientId = await HandleWebSocketAsync(webSocket, roomId, clientType, context);
        }
        finally
        {
            if (clientType.Equals("Broadcaster"))
            {
                try
                {
                    await _connectionManager.RemoveBroadcaster(webSocket, roomId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[RTCSocketMiddleware] 브로드캐스터 해제 실패: {roomId}");
                    
                    // 에러 로그 DB에 저장 
                    try
                    {
                        await SaveErrorLogToDbAsync("RTCSocketMiddleware.RemoveBroadcaster", ex.Message, ex.StackTrace);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "[RTCSocketMiddleware] 에러 로그 저장 실패 (무시됨)");
                    }
                }
            }

            if (clientType.Equals("Client")) 
                _connectionManager.RemoveClient(clientId);
        }
    }
    
    public async Task<string> HandleWebSocketAsync(WebSocket socket, string roomId, string clientType, HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        _logger.LogInformation($"[RTCSocketMiddleware] {clientIp} 연결");
    
        var buffer = new byte[1024 * 8];

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var cancellationToken = cts.Token;

        string clientId = string.Empty;
        
        try
        {
            // 메시지 루프
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug($"[RTCSocketMiddleware] 타임아웃으로 연결 종료: <{roomId}>{clientType} - {clientIp}");
                    break;
                }
                
                // 종료요청처리
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogDebug($"[RTCSocketMiddleware] 종료 요청: <{roomId}>{clientType} - {clientIp}");
    
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "서버에서 연결을 종료합니다.",
                        CancellationToken.None);
                    break;
                }
    
                // 메시지 처리
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var received = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogDebug("[RTCSocketMiddleware] Received: " + received);
    
                    try
                    {
                        // 메시지 역직렬화
                        var message = JsonSerializer.Deserialize<WebSocketMessage>(received);
    
                        if (message == null)
                        {
                            await SendErrorAsync(socket, "잘못된 메시지 형식입니다.");
                            continue;
                        }

                        if (message.Type.Equals("Join"))
                        {
                            // join 처리는 별도로 진행
                            if (clientType.Equals("Broadcaster"))
                            {
                                try
                                {
                                    await _connectionManager.AddBroadcaster(socket, roomId);

                                    await SendMessageAsync(socket, new WebSocketMessage{Type="System", Payload="브로드캐스터 등록 완료", Timestamp = DateTimeOffset.UtcNow});
                                    _logger.LogDebug("[RTCSocketMiddleware] broadcasterList : " + _connectionManager.GetBroadcasters().Count);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError("[RTCSocketMiddleware] 브로드 캐스터 등록 중 오류 발생 : "  + ex.Message);
                                    
                                    // 에러 로그 DB에 저장 
                                    try
                                    {
                                        await SaveErrorLogToDbAsync("RTCSocketMiddleware.AddBroadcaster", ex.Message, ex.StackTrace);
                                    }
                                    catch (Exception logEx)
                                    {
                                        _logger.LogWarning(logEx, "[RTCSocketMiddleware] 에러 로그 저장 실패 (무시됨)");
                                    }
                                    
                                    await SendErrorAsync(socket, $"등록 처리 중 오류 발생하였습니다.");
                                }
                            }
                            else if (clientType.Equals("Client"))
                            {
                                clientId = _connectionManager.RegisterClient(socket, roomId);

                                var clientMessage = new WebSocketMessage
                                {
                                    Type = "Joined",
                                    Payload = clientId,
                                    Timestamp = DateTimeOffset.UtcNow
                                };
                                await SendMessageAsync(socket, clientMessage);
                            }
                            else
                            {
                                _logger.LogError("[RTCSocketMiddleware] 잘못된 클라이언트 타입");
                                await SendErrorAsync(socket, $"클라이언트 타입은 Broadcaster 와 Client 만 가능합니다.");
                            }
                        }
                        else
                        {
                            // 핸들러 타입별 처리 후 처리결과 전송
                            if (_handlers.TryGetValue(message.Type, out var handler))
                            {
                                var response = await handler.HandleAsync(message);

                                if (response != null)
                                {
                                    await SendMessageAsync(socket, response);
                                }
                            }
                            else
                            {
                                _logger.LogError($"[WebSocketMiddleware] 알 수 없는 메시지 타입: {message.Type}");
                                await SendErrorAsync(socket, $"지원하지 않는 메시지 타입입니다: {message.Type}");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"[WebSocketMiddleware] JSON 파싱 오류: {ex.Message}");
                        
                        // 에러 로그 DB에 저장 
                        try
                        {
                            await SaveErrorLogToDbAsync("RTCSocketMiddleware.JsonParsing", ex.Message, ex.StackTrace);
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogWarning(logEx, "[RTCSocketMiddleware] 에러 로그 저장 실패 (무시됨)");
                        }
                        
                        await SendErrorAsync(socket, "메시지 파싱 오류");
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogError($"[WebSocketMiddleware] WebSocket 오류: {ex.Message}");
            
            // 에러 로그 DB에 저장 
            try
            {
                await SaveErrorLogToDbAsync("RTCSocketMiddleware.WebSocketException", ex.Message, ex.StackTrace);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "[RTCSocketMiddleware] 에러 로그 저장 실패 (무시됨)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[WebSocketMiddleware] 예상치 못한 오류: {ex.Message}");
            
            // 에러 로그 DB에 저장 
            try
            {
                await SaveErrorLogToDbAsync("RTCSocketMiddleware.UnexpectedException", ex.Message, ex.StackTrace);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "[RTCSocketMiddleware] 에러 로그 저장 실패 (무시됨)");
            }
        }
        finally
        {
            _logger.LogInformation($"[WebSocketMiddleware] WebSocket 연결 종료: <{roomId}>{clientId}");
        }
        
        return clientId;
    }
    
    // 메시지 전송
    private async Task SendMessageAsync(WebSocket socket, WebSocketMessage message)
    {
        if (socket.State != WebSocketState.Open)
        {
            _logger.LogWarning($"[RTCSocketMiddleware] 소켓이 열려있지 않습니다. 상태: {socket.State}");
            return;
        }
        
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
    
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    
    // 에러 전송
    private async Task SendErrorAsync(WebSocket socket, string message)
    {
        var errorResponse = new WebSocketMessage
        {
            Type = "error",
            Payload = message,
            Timestamp =  DateTimeOffset.UtcNow
        };
        
        await SendMessageAsync(socket, errorResponse);
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
                ErrorCode = "E004", // 시스템 오류
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
            _logger.LogError(ex, "[RTCSocketMiddleware] 에러 로그 DB 저장 실패");
        }
    }
}