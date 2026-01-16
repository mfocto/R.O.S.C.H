using R.O.S.C.H.WS.Models;

namespace R.O.S.C.H.WS.Common;

public interface IMessageHandler
{
    string MessageType { get; } 
    Task<WebSocketMessage?> HandleAsync(WebSocketMessage message);
}