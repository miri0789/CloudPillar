public class RunDiagnosticsSettings
{
    public string FilePath { get; set; } = "c:/my_diagnostics/my_file.txt";
    public int FleSizBytes { get; set; } = 131072;
    public int PeriodicResponseWaitSeconds { get; set; } = 10;
    public int ResponseTimeoutMinutes { get; set; } = 5;
}