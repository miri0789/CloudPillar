public class FileRestrictionDetails
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Root { get; set; }
    public int? MaxSize { get; set; }
    public List<string>? AllowPatterns { get; set; }
    public List<string>? DenyPatterns { get; set; }
}