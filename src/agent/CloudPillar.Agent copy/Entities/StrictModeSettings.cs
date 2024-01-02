public class StrictModeSettings
{
    public bool StrictMode { get; set; }
    public string? ProvisionalAuthenticationMethods { get; set; }
    public string? PermanentAuthenticationMethods { get; set; }
    public List<string>? GlobalPatterns { get; set; }
    public List<FileRestrictionDetails>? FilesRestrictions { get; set; }
}