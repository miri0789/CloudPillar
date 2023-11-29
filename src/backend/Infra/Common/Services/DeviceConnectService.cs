using Shared.Logger;
using Microsoft.Azure.Devices;
using Backend.Infra.Wrappers;
using Polly;

namespace Backend.Infra.Common;

public class DeviceConnectService : IDeviceConnectService
{
    private readonly ILoggerHandler _logger;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ICommonEnvironmentsWrapper _environmentsWrapper;

    public DeviceConnectService(ILoggerHandler logger, IDeviceClientWrapper deviceClientWrapper, ICommonEnvironmentsWrapper environmentsWrapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
    }

    public ServiceClient CreateFromConnectionString(string connString)
    {
        return _deviceClientWrapper.CreateFromConnectionString(connString);
    }


    public async Task SendMessage(ServiceClient serviceClient, Message c2dMessage, string deviceId)
    {
        try
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(_environmentsWrapper.retryPolicyExponent, retryAttempt => TimeSpan.FromSeconds(Math.Pow(_environmentsWrapper.retryPolicyBaseDelay, retryAttempt)),
                (ex, time) => _logger.Warn($"Failed to send message. Retrying in {time.TotalSeconds} seconds... Error details: {ex.Message}"));
            await retryPolicy.ExecuteAsync(async () => await _deviceClientWrapper.SendAsync(serviceClient, deviceId, c2dMessage));
            _logger.Info($"Blobstreamer SendMessage success. message title: {c2dMessage.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Blobstreamer SendMessage failed. Message: {ex.Message}");
        }
    }

}
