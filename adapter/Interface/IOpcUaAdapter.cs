namespace R.O.S.C.H.adapter.Interface;

public interface IOpcUaAdapter
{
    Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct);
    Task WriteStateAsync(CancellationToken ct, string channel, string device, string tag, object value);
    void Dispose();
}