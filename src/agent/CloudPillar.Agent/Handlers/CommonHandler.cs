using Microsoft.Azure.Devices.Client;
using CloudPillar.Agent.Interfaces;

namespace CloudPillar.Agent.Handlers;

public class CommonHandler : ICommonHandler
{
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    public CommonHandler(IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        _environmentsWrapper = environmentsWrapper;
    }

    /// <summary>
    /// Gets the device ID from the connection string.
    /// </summary>
    /// <param name="connectionString">Device connection string.</param>
    /// <returns>Device ID.</returns>
    public string GetDeviceIdFromConnectionString(string connectionString)
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

    /// <summary>
    /// Retrieves the transport type from the TRANSPORT_TYPE environment variable, or defaults to AMQP.
    /// </summary>
    /// <returns>The transport type.</returns>
    public TransportType GetTransportType()
    {
        var transportTypeString = _environmentsWrapper.transportType;
        return Enum.TryParse(transportTypeString, out TransportType transportType)
            ? transportType
            : TransportType.Amqp;
    }
}
