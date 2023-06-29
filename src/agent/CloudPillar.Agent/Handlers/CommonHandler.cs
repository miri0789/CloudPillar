using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Handlers;
public interface ICommonHandler
{
    string GetDeviceIdFromConnectionString(string connectionString);
    TransportType GetTransportType();
}
public class CommonHandler: ICommonHandler
{
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
        var transportTypeString = Environment.GetEnvironmentVariable("TRANSPORT_TYPE");
        return Enum.TryParse(transportTypeString, out TransportType transportType)
            ? transportType
            : TransportType.Amqp;
    }
}
