public static class StrictModeMockHelper
{
    public static string ROOT = "c:/";
    public static string ROOT_UPLOAD_UPPERCASE = "C:/demoUpload";
    public static string ROOT_GLOBAL = "c:/globalUpload";
    public static string ROOT_UPLOAD = "c:/demoUpload";
    public static string ROOT_DOWNLOAD = "c:/demoDownload";
    public static string DOWNLOAD = "Download";
    public static string UPLOAD = "Upload";
    public static string UPLOAD_KEY = "LogUploadAllow";
    public static string DOWNLOAD_KEY = "LogDownloadAllow";

    public static StrictModeSettings SetStrictModeSettingsValueMock(bool strictMode = true)
    {
        var globalPatterns = new List<string>
        {
            $"{ROOT_GLOBAL}/*.txt"
        };
        var uploadRestrictionDetails = new FileRestrictionDetails()
        {
            Id = "LogUploadAllow",
            Type = "Upload",
            Root = ROOT_UPLOAD,
            AllowPatterns = new List<string>
                {
                    "/*.txt"
                },
            DenyPatterns = new List<string>() // Add any deny patterns here if needed
        };
        var downloadRestrictionDetails = new FileRestrictionDetails()
        {
            Id = "LogDownloadAllow",
            Type = "Download",
            Root = ROOT_DOWNLOAD,
            MaxSize = 1,
            AllowPatterns = new List<string>
                {
                    "**/*.log",
                    "*/*.png",
                    "*.txt"
                },
            DenyPatterns = new List<string>() // Add any deny patterns here if needed
        };

        return new StrictModeSettings()
        {
            StrictMode = strictMode,
            GlobalPatterns = globalPatterns,
            FilesRestrictions = new List<FileRestrictionDetails> { uploadRestrictionDetails, downloadRestrictionDetails }
        };

    }

}