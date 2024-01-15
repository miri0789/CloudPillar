namespace PriorityQueue.Services.Interfaces;
public interface IMessageProcessor
{
    // return true is the original message shall be consumed, and false if it shall retain
    Task<bool> ProcessMessageAsync(string message, IDictionary<string, string> properties);
}
