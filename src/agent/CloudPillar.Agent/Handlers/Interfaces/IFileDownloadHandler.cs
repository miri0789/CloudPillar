using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler
{
    Task InitFileDownloadAsync(DownloadAction downloadAction, ActionToReport actionToReport/*, TwinPatchChangeSpec changeSpecKey = TwinPatchChangeSpec.ChangeSpec*/);
    Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message);
}