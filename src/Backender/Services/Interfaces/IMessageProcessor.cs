using Backender.Entities.Enums;

namespace Backender.Services.Interfaces;
public interface IMessageProcessor
{
    Task<(MessageProcessType type, string? response, IDictionary<string, string>? responseHeaers)>
    ProcessMessageAsync(string message, IDictionary<string, string> properties, CancellationToken stoppingToken);
}
