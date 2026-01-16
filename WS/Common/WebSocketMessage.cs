namespace R.O.S.C.H.WS.Models;

public class WebSocketMessage
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string? senderId { get; set; }
}