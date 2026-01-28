namespace R.O.S.C.H.Database.Models;

public class Code
{
    public int code_id { get; set; }
    public string type { get; set; } = string.Empty;
    public string code { get; set; } = string.Empty;
    public string code_name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public bool is_active { get; set; }
    public DateTimeOffset created_at { get; set; }
    public DateTimeOffset updated_at { get; set; }
}