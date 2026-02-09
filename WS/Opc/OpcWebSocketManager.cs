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

public class OpcWebSocketManager
{
    private static readonly ConcurrentDictionary<string, OpcClientDto> _clients = new();
    private static readonly ConcurrentDictionary<string, OpcClientDto> _unity = new();
    private readonly ILogger<OpcWebSocketManager> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _config;
    
    public OpcWebSocketManager(ILogger<OpcWebSocketManager> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _config = config;
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
                
                // 에러 로그 DB에 저장 
                try
                {
                    await SaveErrorLogToDbAsync("OpcWebSocketManager.SendDataAsync(Client)", e.Message, e.StackTrace);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "[OpcWebSocketManager] 에러 로그 저장 실패 (무시됨)");
                }
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
                
                // 에러 로그 DB에 저장 
                try
                {
                    await SaveErrorLogToDbAsync("OpcWebSocketManager.SendDataAsync(Unity)", e.Message, e.StackTrace);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "[OpcWebSocketManager] 에러 로그 저장 실패 (무시됨)");
                }
            }
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
            _logger.LogError(ex, "[OpcWebSocketManager] 에러 로그 DB 저장 실패");
        }
    }
}