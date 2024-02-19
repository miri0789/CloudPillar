public class StrictModeSettings
{
    public bool StrictMode { get; set; }
    public bool AllowHTTPAPI { get; set; } = false;
    public string? ProvisionalAuthenticationMethods { get; set; }
    public string? PermanentAuthenticationMethods { get; set; }
    public List<string>? GlobalPatterns { get; set; }
    public List<FileRestrictionDetails>? FilesRestrictions { get; set; }
}