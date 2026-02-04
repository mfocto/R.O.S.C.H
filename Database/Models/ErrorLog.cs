namespace R.O.S.C.H.Database.Models;

public class ErrorLog
{
    public long ErrorId { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorSource { get; set; } = string.Empty;
    public string ErrorMsg { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public int UserId { get; set; }
    public int DeviceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}