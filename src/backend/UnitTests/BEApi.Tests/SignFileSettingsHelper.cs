using Shared.Entities.Twin;

public static class DownloadSettingsHelper
{
    public static DownloadSettings SetDownloadSettingsValueMock()
    {
        return new DownloadSettings()
        {
            SignFileBufferSize = 8192
        };
    }
}