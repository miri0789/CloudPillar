public class AppSettings
{
    public bool StrictMode { get; set; }
    public string ProvisionalAuthentucationMethods { get; set; }
    public string PermanentAuthentucationMethods { get; set; }
    public List<Dictionary<string, FileRestrictionDetails>> FilesRestrictions { get; set; }
}

public class FileRestrictionDetails
{
    public string Type { get; set; }
    public string Root { get; set; }
    // public int? Size { get; set; }
    public List<string> AllowPatterns { get; set; }
    public List<string> DenyPatterns { get; set; }
}