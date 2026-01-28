namespace R.O.S.C.H.Database.Models;

public class ErrorLog
{
    public long error_id { get; set; }
    public string error_code { get; set; } = string.Empty;
    public string error_source { get; set; } = string.Empty;
    public string error_msg { get; set; } = string.Empty;
    public string? stack_trace { get; set; }
    public int user_id { get; set; }
    public int device_id { get; set; }
    public DateTimeOffset created_at { get; set; }
}