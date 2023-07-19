using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Wrappers;
public class DeviceClientWrapper : IDeviceClientWrapper
{

    private readonly IEnvironmentsWrapper _environmentsWrapper;

    private DeviceClientWrapper(IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        _environmentsWrapper = environmentsWrapper;
    }

    public DeviceClient CreateDeviceClient(string connectionString)
    {
        try
        {
            TransportType _transportType = GetTransportType();
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, _transportType);
            if (deviceClient == null)
            {
                Console.WriteLine($"CreateDeviceClient FromConnectionString failed the device is null");
            }
            return deviceClient;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateFromConnectionString failed {ex.Message}");
            throw;

        }
    }


    public string GetDeviceId(string connectionString)
    {
        var items = connectionString.Split(';');
        foreach (var item in items)
        {
            if (item.StartsWith("DeviceId"))
            {
                return item.Split('=')[1];
            }
        }

        throw new ArgumentException("DeviceId not found in the connection string.");
    }


    public TransportType GetTransportType()
    {
        var transportTypeString = _environmentsWrapper.transportType;
        return Enum.TryParse(transportTypeString, out TransportType transportType)
            ? transportType
            : TransportType.Amqp;
    }

    public Task<Message> ReceiveAsync(CancellationToken cancellationToken, DeviceClient deviceClient)
    {
        return deviceClient.ReceiveAsync(cancellationToken);
    }


    public async Task SendEventAsync(Message message, DeviceClient deviceClient)
    {
        await deviceClient.SendEventAsync(message);
    }


    public async Task CompleteAsync(Message message, DeviceClient deviceClient)
    {
        await deviceClient.CompleteAsync(message);
    }

}
