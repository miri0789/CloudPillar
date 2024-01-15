using System.Text;
using PriorityQueue.Services.Interfaces;
using Shared.Logger;

namespace PriorityQueue.Services;
public class MessageProcessor : IMessageProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerHandler _logger;

    public MessageProcessor(HttpClient httpClient,
                            ILoggerHandler logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ProcessMessageAsync(string message, IDictionary<string, string> properties)
    {
        _logger.Debug($"{DateTime.Now} Processing message: {message}");
        await Task.Delay(7000);
        // var backendUrl = Environment.GetEnvironmentVariable("SVC_BACKEND_URL") ?? "http://localhost:5000/api/ProcessMessage";
        // var request = new HttpRequestMessage(HttpMethod.Post, backendUrl)
        // {
        //     Content = new StringContent(message, Encoding.UTF8, "application/json")
        // };

        // foreach (var property in properties)
        // {
        //     request.Headers.Add(property.Key, property.Value);
        // }
        // try
        // {
        //     var response = await _httpClient.SendAsync(request);

        //     if (!response.IsSuccessStatusCode)
        //     {
        //         return ((int)response.StatusCode) / 100 == 4; // Processed if 4xx the request is bad
        //     }
        //     else
        //     {
        //         return true; // Processed
        //     }
        // }
        // catch (Exception ex)
        // {
            return true;
        // }

    }
}
