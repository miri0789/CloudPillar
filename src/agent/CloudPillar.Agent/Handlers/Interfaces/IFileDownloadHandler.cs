﻿using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler
{
    bool AddFileDownload(ActionToReport actionToReport);
    Task InitFileDownloadAsync(ActionToReport actionToReport, CancellationToken cancellationToken);
    Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken);
    void InitDownloadsList(List<ActionToReport> actions = null);
}