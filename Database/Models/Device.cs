namespace R.O.S.C.H.Database.Models;

public class Device
{
    public int DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceAlias { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}