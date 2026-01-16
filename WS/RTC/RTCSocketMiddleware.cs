using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using R.O.S.C.H.WS.Common;
using R.O.S.C.H.WS.Models;

namespace R.O.S.C.H.WS.RTC;

public class RTCSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RTCConnectionManager _connectionManager;
    private readonly ILogger<RTCSocketMiddleware> _logger;
    private readonly Dictionary<string, IMessageHandler> _handlers;

    public RTCSocketMiddleware(
        RequestDelegate next,
        RTCConnectionManager connectionManager,
        ILogger<RTCSocketMiddleware> logger,
        IEnumerable<IMessageHandler> handlers)
    {
        _next = next;
        _connectionManager = connectionManager;
        _logger = logger;
        _handlers = handlers.ToDictionary(h => h.MessageType,  h => h);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }
        
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        var clientType = context.Request.Query["type"].ToString();
        var clientId = context.Request.Query["clientId"].ToString();
        
        var sessionId = _connectionManager.AddConnection(webSocket, clientType, clientId);

        try
        {
            await HandleWebSocketAsync(webSocket, sessionId, context);
        }
        finally
        {
            _connectionManager.RemoveConnection(sessionId, clientType, clientId);
        }
    }

    public async Task HandleWebSocketAsync(WebSocket socket, string sessionId, HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        _logger.LogInformation($"[RTCSocketMiddleware] {clientIp}:{sessionId} 연결 수립");

        var buffer = new byte[1024 * 4];

        var IsHandShake = false;
        var handShakeTimeout = TimeSpan.FromMinutes(1);
        var connectionStart = DateTimeOffset.Now;

        try
        {
            // 메시지 루프
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                // 종료요청처리
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation($"[RTCSocketMiddleware] 클라이언트 종료 요청: {sessionId}");

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
                        var message = JsonSerializer.Deserialize<WebSocketMessage>(received);

                        if (message != null)
                        {
                            await SendErrorAsync(socket, "잘못된 메시지 형식입니다.");
                            continue;
                        }

                        // 핸드셰이크 여부 확인
                        if (!IsHandShake)
                        {
                            // 타임아웃 체크
                            if (DateTime.UtcNow - connectionStart > handShakeTimeout)
                            {
                                await SendErrorAsync(socket, "핸드셰이크 타임아웃.");
                                await socket.CloseAsync(
                                    WebSocketCloseStatus.NormalClosure,
                                    "핸드셰이크 타임아웃.",
                                    CancellationToken.None);
                                break;
                            }

                            if (message.Type != "handshake")
                            {
                                await SendErrorAsync(socket, "첫 메시지는 핸드셰이크여야합니다.");
                                continue;
                            }

                            IsHandShake = true;
                        }

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
                    catch (JsonException ex)
                    {
                        _logger.LogError($"[WebSocketMiddleware] JSON 파싱 오류: {ex.Message}");
                        await SendErrorAsync(socket, "메시지 파싱 오류");
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogError($"[WebSocketMiddleware] WebSocket 오류: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[WebSocketMiddleware] 예상치 못한 오류: {ex.Message}");
        }
        finally
        {
            _logger.LogInformation($"[WebSocketMiddleware] WebSocket 연결 종료: {sessionId}");
        }
    }

    private async Task SendMessageAsync(WebSocket socket, WebSocketMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task SendErrorAsync(WebSocket socket, string message)
    {
        var errorResponse = new WebSocketMessage
        {
            Type = "error",
            Payload = message,
            Timestamp =  DateTimeOffset.Now
        };
    }
}