using System.Net.WebSockets;

namespace R.O.S.C.H.WS.Opc.DTO;

public class OpcClientDto
{
    public string ClientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public WebSocket Socket { get; set; }
    public DateTimeOffset ConnectedAt { get; set; }

    public OpcClientDto(string type, WebSocket socket)
    {
        ClientId = Guid.NewGuid().ToString();
        Type = type;
        Socket = socket;
        ConnectedAt = DateTimeOffset.UtcNow;
    }
}