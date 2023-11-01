using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;

public static class StrictModeMockHelper
{
    public static string ROOT_UPLOAD = "c:/demoUpload/";
    public static string ROOT_DOWNLOAD = "c:/demoDownload/";
    public static string DOWNLOAD = "Download";
    public static string UPLOAD = "Upload";
    public static string UPLOAD_KEY = "LogUploadAllow";
    public static string DOWNLOAD_KEY = "LogDownloadAllow";

    public static StrictModeSettings SetStrictModeSettingsValueMock()
    {
        var uploadRestrictionDetails = new FileRestrictionDetails()
        {
            Id = "LogUploadAllow",
            Type = "Upload",
            Root = ROOT_UPLOAD,
            AllowPatterns = new List<string>
                {
                    "*.txt"
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
            StrictMode = true,
            FilesRestrictions = new List<FileRestrictionDetails> { uploadRestrictionDetails, downloadRestrictionDetails }
        };

    }

}