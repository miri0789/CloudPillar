﻿using shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IMessageSubscriber
{
    Task HandleMessage(BaseMessage message);
}
