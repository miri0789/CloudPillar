using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;

public static class SignFileSettingsHelper
{
    public static SignFileSettings SetSignFileSettingsValueMock()
    {
        return new SignFileSettings()
        {
            BufferSize = 8192
        };
    }
}