using System.Collections.Concurrent;
using System.Net.WebSockets;
using R.O.S.C.H.WS.Common;

namespace R.O.S.C.H.WS.RTC;

public class RTCConnectionManager(ILogger<RTCConnectionManager> logger)
{
    private ILogger _logger = logger;
    public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ClientInfo>> _clientInfos = new ();
    public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _unity = new ();
    public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _clients = new ();
    public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _pairs = new ();
     
    // 연결처리 
    public string AddConnection(WebSocket socket, string type, string clientId)
    {
        var sessionId = Guid.NewGuid().ToString();
        var connection = new ConcurrentDictionary<string, WebSocket>();
        connection.TryAdd(clientId, socket);
        
        if (type.Equals("Unity"))
        {
            _unity.TryAdd(sessionId, connection);
            _pairs.TryAdd(clientId, new ConcurrentDictionary<string, byte>());  
        } else if (type.Equals("Client"))
        {
            _clients.TryAdd(sessionId, connection);
        }
        
        var clientInfo = new ConcurrentDictionary<string, ClientInfo>();

        clientInfo.TryAdd(clientId, new ClientInfo
        {
            SessionId = sessionId,
            ClientId = clientId,
            LastActivate = DateTimeOffset.Now
        });

        _clientInfos.TryAdd(sessionId, clientInfo);
        
        _logger.LogInformation($"[RTCConnectionManager] ({clientId}) 연결되었습니다.");

        return sessionId;
    }

    // 연결해제
    public void RemoveConnection(string sessionId, string type, string clientId)
    {
        ConcurrentDictionary<string, WebSocket> clients;
        ConcurrentDictionary<string, ClientInfo> clientInfos =  _clientInfos[sessionId];
        if (type.Equals("Unity"))
        {
            clients = _unity[sessionId];
            _pairs.TryRemove(clientId, out _);
        }  else 
        {
            clients = _clients[sessionId];
        }
        
        clients.TryRemove(clientId, out _);
        clientInfos.TryRemove(clientId, out _);
        
        _logger.LogInformation($"[RTCConnectionManager] ({clientId}) 연결 해제되었습니다.");
    }

    // 정보조회
    public ClientInfo? GetClientInfo(string sessionId, string clientId)
    {
        if (_clientInfos.TryGetValue(sessionId, out var clientInfos))
        {
            clientInfos.TryGetValue(clientId, out var info);
            return info;
        }

        return null;
    }

    // 페어링
    public void Pair(string sessionId, string clientId, string pairId)
    {
        if (_clientInfos[sessionId].TryGetValue(clientId, out var info))
        {
            // 기존 페어링이 있으면 해제
            if (string.IsNullOrWhiteSpace(info.PairId))
            {
                _pairs[pairId].TryRemove(info.PairId, out _);
                info.PairId = string.Empty;
            }

            _pairs[pairId].TryAdd(clientId, 0);
            info.PairId = pairId;
        }
    }

    public ConcurrentDictionary<string, byte>? GetPairInfo(string pairId)
    {
        _pairs.TryGetValue(pairId, out var info);
        
        return info;    
    }
    
    
}

