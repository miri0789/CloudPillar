using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public class DeviceClientWrapper : IDeviceClientWrapper
{
    private readonly DeviceClient _deviceClient;
    private readonly IEnvironmentsWrapper _environmentsWrapper;

    public DeviceClientWrapper(IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        _environmentsWrapper = environmentsWrapper;
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
            Console.WriteLine($"CreateFromConnectionString failed {ex.Message}");
            throw;
        }

    }

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

    public Task<Message> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _deviceClient.ReceiveAsync(cancellationToken);
    }


    public async Task SendEventAsync(Message message)
    {
        await _deviceClient.SendEventAsync(message);
    }


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
        updatedReportedProperties[key] = value;
        await _deviceClient.UpdateReportedPropertiesAsync(updatedReportedProperties);
    }

}
