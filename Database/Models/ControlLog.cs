namespace R.O.S.C.H.Database.Models;

public class ControlLog
{
    public long LogId { get; set; }
    public int UserId { get; set; }
    public int DeviceId { get; set; }
    public string ControlType { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string NewValue { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}