using System;
using System.Diagnostics;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler: IMessageSubscriber
{
    Task InitFileDownloadAsync(Guid actionGuid, string path, string fileName);
    Task HandleMessageAsync(BaseMessage message);
}