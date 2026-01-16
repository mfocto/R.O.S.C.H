namespace R.O.S.C.H.WS.Common;

/// <summary>
/// 클라이언트 연결정보
/// </summary>
public class ClientInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientType { get; set; }
    public string PairId { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset LastActivate { get; set; } = DateTimeOffset.Now;
}
