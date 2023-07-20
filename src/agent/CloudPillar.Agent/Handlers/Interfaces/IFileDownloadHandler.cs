using System;
using System.Diagnostics;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler
{
    Task InitFileDownloadAsync(Guid actionGuid, string path, string fileName);
    Task DownloadMessageDataAsync(DownloadBlobChunkMessage downloadBlobChunkMessage);
}