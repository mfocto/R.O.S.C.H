using System.Net.WebSockets;

namespace R.O.S.C.H.WS.RTC.DTO;

public class ClientDTO
{
    public string ClientId { get; set; } = string.Empty;
    public required WebSocket socket { get; set; }
    public string? RoomId { get; set; }
    
    // JOINED, OFFER_SEND, ANSWER_RECEIVED, CONNECTED, DISCONNECTED
    public string status {get; set;} = "DISCONNECTED";
    public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.Now;
}