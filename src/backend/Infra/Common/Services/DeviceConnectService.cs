using Shared.Logger;
using Microsoft.Azure.Devices;
using Polly;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;

namespace Backend.Infra.Common.Services;

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

    public async Task SendDeviceMessageAsync(Message c2dMessage, string deviceId)
    {
        await SendDeviceMessagesAsync(new Message[] { c2dMessage }, deviceId);
    }

    public async Task SendDeviceMessageAsync(ServiceClient serviceClient, Message c2dMessage, string deviceId)
    {
        await SendMessage(serviceClient, c2dMessage, deviceId);
    }

    public async Task SendDeviceMessagesAsync(Message[] c2dMessages, string deviceId)
    {
        try
        {
            using (var serviceClient = _deviceClientWrapper.CreateFromConnectionString())
            {
                foreach (var msg in c2dMessages)
                {
                    await SendMessage(serviceClient, msg, deviceId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"DeviceConnectService SendDeviceMessagesAsync failed. Message: {ex.Message}");
        }
    }


    private async Task SendMessage(ServiceClient serviceClient, Message c2dMessage, string deviceId)
    {
        try
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(_environmentsWrapper.retryPolicyExponent, retryAttempt => TimeSpan.FromSeconds(_environmentsWrapper.retryPolicyBaseDelay),
                (ex, time) => _logger.Warn($"Failed to send message. Retrying in {time.TotalSeconds} seconds... Error details: {ex.Message}"));
            await retryPolicy.ExecuteAsync(async () => await _deviceClientWrapper.SendAsync(serviceClient, deviceId, c2dMessage));
            _logger.Info($"SendMessage success. message title: {c2dMessage.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"DeviceConnectService SendMessage failed. Message: {ex.Message}");
            throw ex;
        }
    }

}
