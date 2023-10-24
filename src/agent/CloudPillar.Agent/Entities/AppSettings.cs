public class AppSettings
{
    public bool StrictMode { get; set; }
    public string ProvisionalAuthentucationMethods { get; set; }
    public string PermanentAuthentucationMethods { get; set; }
    public List<string> GlobalPatterns { get; set; }
    public List<FileRestrictionDetails> FilesRestrictions { get; set; }

}

public class FileRestrictionDetails
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Root { get; set; }
    public int? MaxSize { get; set; }
    public List<string> AllowPatterns { get; set; }
    public List<string> DenyPatterns { get; set; }
}