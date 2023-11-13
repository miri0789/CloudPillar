
using Microsoft.Azure.Devices;

namespace Shared.Entities.Twin;

public class DownloadAction : TwinAction
{
    public string Source { get; set; }
    public string DestinationPath { get; set; }
    public TransportType[]? Protocols { get; set; } = { };

    public DownloadAction()
    {
        this.Action = TwinActionType.SingularDownload;
    }
}

