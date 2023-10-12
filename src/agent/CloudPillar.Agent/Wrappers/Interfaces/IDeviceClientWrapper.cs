using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{
    Task DeviceInitializationAsync(string hostname, IAuthenticationMethod authenticationMethod, CancellationToken cancellationToken);
    Task<bool> IsDeviceInitializedAsync(CancellationToken cancellationToken);
    string GetDeviceId();

    TransportType GetTransportType();
    int GetChunkSizeByTransportType();

    Task SendEventAsync(Message message);

    Task<Message> ReceiveAsync(CancellationToken cancellationToken);

    Task CompleteAsync(Message message);

    Task<Twin> GetTwinAsync(CancellationToken cancellationToken);

    Task UpdateReportedPropertiesAsync(string key, object value);

    Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties);

    Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken = default);
    Task<Uri> GetBlobUriAsync(FileUploadSasUriResponse sasUri, CancellationToken cancellationToken = default);
    Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken = default);
    Task CompleteFileUploadAsync(string correlationId, bool isSuccess, CancellationToken cancellationToken = default);

    ProvisioningTransportHandler GetProvisioningTransportHandler();

}
