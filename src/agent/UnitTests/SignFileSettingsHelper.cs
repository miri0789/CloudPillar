using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;

public static class DownloadSettingsHelper
{
    public static DownloadSettings SetDownloadSettingsValueMock()
    {
        return new DownloadSettings()
        {
            BufferSize = 8192
        };
    }
}