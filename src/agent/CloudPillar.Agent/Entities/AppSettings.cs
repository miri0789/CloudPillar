public class AppSettings
{
    public bool StrictMode { get; set; }
    public string ProvisionalAuthentucationMethods { get; set; }
    public string PermanentAuthentucationMethods { get; set; }
    public FilesRestrictions filesRestrictions { get; set; }
}
public class FilesRestrictions
{
    public Dictionary<string, Restrictions> Restrictions { get; set; }
}

public class Restrictions
{
    public string Type { get; set; }
    public string Root { get; set; }
    public int? Size { get; set; }
    public List<string> AllowPatterns { get; set; }
    public List<string> DenyPatterns { get; set; }
}