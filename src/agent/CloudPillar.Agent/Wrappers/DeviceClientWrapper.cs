using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Shared.Logger;

namespace CloudPillar.Agent.Wrappers;
public class DeviceClientWrapper : IDeviceClientWrapper
{
    private readonly DeviceClient _deviceClient;
    private readonly IEnvironmentsWrapper _environmentsWrapper;

     private readonly ILoggerHandler _logger;

    /// <summary>
    /// Initializes a new instance of the DeviceClient class
    /// </summary>
    /// <param name="environmentsWrapper"></param>
    /// <exception cref="NullReferenceException"></exception>
    public DeviceClientWrapper(IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
    {
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        _environmentsWrapper = environmentsWrapper;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var _transportType = GetTransportType();
        try
        {
            _deviceClient = DeviceClient.CreateFromConnectionString(_environmentsWrapper.deviceConnectionString, _transportType);
            if (_deviceClient == null)
            {
                throw new NullReferenceException("CreateDeviceClient FromConnectionString failed the device is null");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"CreateFromConnectionString failed {ex.Message}");
            throw;
        }

    }

    /// <summary>
    /// Extracts the device ID from the device connection string
    /// </summary>
    /// <returns>Device Id</returns>
    /// <exception cref="ArgumentException"></exception>
    public string GetDeviceId()
    {
        var items = _environmentsWrapper.deviceConnectionString.Split(';');
        foreach (var item in items)
        {
            if (item.StartsWith("DeviceId"))
            {
                return item.Split('=')[1];
            }
        }

        throw new ArgumentException("DeviceId not found in the connection string");
    }


    public TransportType GetTransportType()
    {
        var transportTypeString = _environmentsWrapper.transportType;
        return Enum.TryParse(transportTypeString, out TransportType transportType)
            ? transportType
            : TransportType.Amqp;
    }

    /// <summary>
    /// Asynchronously waits for a message to be received from the device.
    /// after recived the message, need to exec CompleteAsync function to the message
    /// </summary>
    /// <param name="cancellationToken">used to cancel the operation if needed.</param>
    /// <returns>a task that represents the asynchronous operation and contains a Message when received.</returns>
    public Task<Message> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _deviceClient.ReceiveAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously sends an event message to the device.
    /// </summary>
    /// <param name="message">The message object containing the data to be sent.</param>
    /// <returns>a task representing the asynchronous operation.</returns>
    public async Task SendEventAsync(Message message)
    {
        await _deviceClient.SendEventAsync(message);
    }

    /// <summary>
    /// asynchronously completes the processing of a received message.
    /// </summary>
    /// <param name="message">the message object representing the received message to be completed.</param>
    /// <returns>a task representing the asynchronous operation.</returns>
    public async Task CompleteAsync(Message message)
    {
        await _deviceClient.CompleteAsync(message);
    }

    public async Task<Twin> GetTwinAsync()
    {
        var twin = await _deviceClient.GetTwinAsync();
        return twin;
    }

    public async Task UpdateReportedPropertiesAsync(string key, object value)
    {
        var updatedReportedProperties = new TwinCollection();
        updatedReportedProperties[char.ToLower(key[0]) + key.Substring(1)] = value;
        await _deviceClient.UpdateReportedPropertiesAsync(updatedReportedProperties);
    }

}
