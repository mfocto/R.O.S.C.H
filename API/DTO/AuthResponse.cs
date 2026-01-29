namespace R.O.S.C.H.API.DTO;

public record AuthResponse
{
    public bool Success  { get; set; }
    public string Message  { get; set; }
    public string Role { get; set; }
}