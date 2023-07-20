
using Microsoft.Azure.Devices;

namespace CloudPillar.Agent.Entities.Twin;

public class DownloadAction : TwinAction
{
    public string Source { get; set; }
    public long RetransmissionRewind { get; set; }
    public string DestinationPath { get; set; }
    public TransportType[] Protocols { get; set; }

    
    public DownloadAction()
    {
        this.ActionName = TwinActionType.SingularDownload;
    }
}

