namespace Shared.Entities.Twin;

public class DownloadSettings
{
    public int SignFileBufferSize { get; set; } = 16384;
    public int CommunicationDelaySeconds { get; set; } = 30;
    public int BlockedDelayMinutes { get; set; } = 10;
}