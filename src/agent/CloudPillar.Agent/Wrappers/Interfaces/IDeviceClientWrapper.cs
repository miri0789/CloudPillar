using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{
    void DeviceInitialization(DeviceClient deviceClient);

    string GetDeviceId();

    TransportType GetTransportType();
    int GetChunkSizeByTransportType();

    Task SendEventAsync(Message message);

    Task<Message> ReceiveAsync(CancellationToken cancellationToken);

    Task CompleteAsync(Message message);

    Task<Twin> GetTwinAsync();

    Task UpdateReportedPropertiesAsync(string key, object value);

    Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties);

    Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken = default);
    Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken = default);
    Task CompleteFileUploadAsync(string correlationId, bool isSuccess, CancellationToken cancellationToken = default);

}
