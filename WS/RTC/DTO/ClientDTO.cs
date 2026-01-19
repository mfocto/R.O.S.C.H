using System.Net.WebSockets;

namespace R.O.S.C.H.WS.RTC.DTO;

public class ClientDTO
{
    public string ClientId { get; set; } = string.Empty;
    public required WebSocket Socket { get; set; }
    public string? RoomId { get; set; }
    
    // JOINED, OFFER_SEND, ANSWER_RECEIVED, CONNECTED, DISCONNECTED
    public string Status {get; set;} = "DISCONNECTED";
    
    // 등록시간
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.Now;
    
    // 연결시간
    public DateTimeOffset? ConnectedAt { get; set; }
    
    // 마지막 변경시간
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.Now;
}