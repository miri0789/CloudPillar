using CloudPillar.Agent.Interfaces;
using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent;
public class DeviceClientWrapper : IDeviceClientWrapper
{

    private readonly IEnvironmentsWrapper _environmentsWrapper;

    private DeviceClientWrapper(IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        _environmentsWrapper = environmentsWrapper;
    }

    public DeviceClient CreateDeviceClient()
    {
        try
        {
            string _deviceConnectionString = _environmentsWrapper.deviceConnectionString;
            TransportType _transportType = GetTransportType();
            var _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, _transportType);
            return _deviceClient;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateFromConnectionString failed {ex.Message}");
            throw;

        }
    }


    public string GetDeviceId()
    {
        string _deviceConnectionString = _environmentsWrapper.deviceConnectionString;
        var items = _deviceConnectionString.Split(';');
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




}
