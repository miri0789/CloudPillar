using Backender.Entities.Enums;
using Backender.Entities;

namespace Backender.Services.Interfaces;
public interface IMessageProcessor
{
    Task<(CompletionCode type, string response, IDictionary<string, string>? responseHeaers)>
    ProcessMessageAsync(string message, IDictionary<string, string> properties, CancellationToken cancellationToken);

    Task<HttpResponseMessage> SendPostRequestAsync(string relativeUri, string body, IDictionary<string, string>? headers, CancellationToken cancellationToken);
}
