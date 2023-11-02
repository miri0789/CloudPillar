public class StrictModeSettings
{
    public bool StrictMode { get; set; }
    public string ProvisionalAuthentucationMethods { get; set; }
    public string PermanentAuthentucationMethods { get; set; }
    public List<string> GlobalPatterns { get; set; }
    public List<FileRestrictionDetails> FilesRestrictions { get; set; }
}