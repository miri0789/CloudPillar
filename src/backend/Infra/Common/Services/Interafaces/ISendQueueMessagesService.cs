using Newtonsoft.Json.Linq;

namespace Backend.Infra.Common.Services.Interfaces;

public interface ISendQueueMessagesService
{
    Task SendMessageToQueue(string message);
}