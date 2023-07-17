using shared.Entities.Messages;

namespace CloudPillar.Agent.Interfaces;
public interface IMessageSubscriber
{
    Task HandleMessage(BaseMessage message);
}
