namespace Backend.Infra.Common.Services.Interfaces;

public interface IQueueMessagesService
{
    Task SendMessageToQueue(string message);
    Task GetMessageFromQueue();
}