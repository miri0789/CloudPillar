using System;
using System.Diagnostics;
using CloudPillar.Agent.MessagesSubscribers;
using shared.Entities.Messages;

namespace CloudPillar.Agent.Interfaces;

public interface IFileDownloadHandler: IMessageSubscriber
{
    Task InitFileDownloadAsync(Guid actionGuid, string path, string fileName);
    Task HandleMessage(DownloadBlobChunkMessage downloadBlobChunkMessage);
}