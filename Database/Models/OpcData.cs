namespace R.O.S.C.H.Database.Models;

public class OpcData
{
    public long DataId { get; set; }
    public int DeviceId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string TagValue { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public DateTimeOffset? SourceTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}