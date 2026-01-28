namespace R.O.S.C.H.Database.Models;

public class DeviceTag
{
    public int TagId { get; set; }
    public int DeviceId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string DeviceName  { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string AccessType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}