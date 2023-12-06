using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{
    Task DeviceInitializationAsync(string hostname, IAuthenticationMethod authenticationMethod, CancellationToken cancellationToken);
    Task<bool> IsDeviceInitializedAsync(CancellationToken cancellationToken);
    TransportType GetTransportType();
    int GetChunkSizeByTransportType();

    Task SendEventAsync(Message message, CancellationToken cancellationToken);

    Task<Message> ReceiveAsync(CancellationToken cancellationToken);

    Task CompleteAsync(Message message, CancellationToken cancellationToken);

    Task DisposeAsync();
   
    Task<Twin> GetTwinAsync(CancellationToken cancellationToken);

    Task UpdateReportedPropertiesAsync(string key, object value, CancellationToken cancellationToken);

    Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken);

    Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken = default);
    Task<Uri> GetBlobUriAsync(FileUploadSasUriResponse sasUri, CancellationToken cancellationToken = default);
    Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken = default);
    Task CompleteFileUploadAsync(string correlationId, bool isSuccess, CancellationToken cancellationToken = default);

    ProvisioningTransportHandler GetProvisioningTransportHandler();

    Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, CancellationToken cancellationToken = default);

}
