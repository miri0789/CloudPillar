using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IMessageSubscriber
{
    Task<ActionToReport?> HandleMessageAsync(BaseMessage message);
}
