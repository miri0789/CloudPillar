using System;
using System.Diagnostics;
using shared.Entities.Messages;

namespace CloudPillar.Agent.Interfaces;

public interface IFileDownloadHandler
{
    Task InitFileDownloadAsync(Guid actionGuid, string path, string fileName);
    Task DownloadMessageDataAsync(DownloadBlobChunkMessage downloadBlobChunkMessage, byte[] messageData);
}