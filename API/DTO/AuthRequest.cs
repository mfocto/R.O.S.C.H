namespace R.O.S.C.H.API.DTO;

public record AuthRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}