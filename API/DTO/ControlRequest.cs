namespace R.O.S.C.H.API.DTO;

public record ControlRequest
{
    public string Name { get; set; }
    public object Value { get; set; }
    public string UserName { get; set; }
    
}