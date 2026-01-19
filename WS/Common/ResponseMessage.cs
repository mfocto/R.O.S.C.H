namespace R.O.S.C.H.WS.Common;

public class ResponseMessage
{
    public bool Success { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}