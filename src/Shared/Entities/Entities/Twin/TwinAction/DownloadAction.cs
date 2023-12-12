
using Microsoft.Azure.Devices;

namespace Shared.Entities.Twin;

public class DownloadAction : TwinAction
{
    public string Source { get; set; }
    public string DestinationPath { get; set; }
    public bool Unzip { get; set; }

    public DownloadAction()
    {
        Action = TwinActionType.SingularDownload;
    }
}

