public class RunDiagnosticsSettings
{
    public int DeleteBlobAfterHours { get; set; } = 1;
    public string DestinationPathForDownload { get; set; } = "c:/my_diagnostics/my_download_file.txt";
}