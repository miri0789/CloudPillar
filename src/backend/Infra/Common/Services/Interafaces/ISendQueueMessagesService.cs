namespace Backend.Infra.Common.Services.Interfaces;

public interface ISendQueueMessagesService
{
    Task SendMessageToQueue(string url, object message = null);
}