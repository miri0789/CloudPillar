
using System.Security.Cryptography.X509Certificates;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFileDownloadEventAsync(CancellationToken cancellationToken, string changeSpecId, string fileName, int actionIndex, string CompletedRanges = "", long? startPosition = null, long? endPosition = null);
    Task SendStreamingUploadChunkEventAsync(byte[] buffer, Uri storageUri, long currentPosition, string checkSum, CancellationToken cancellationToken, bool isRunDiagnostics = false);
    Task ProvisionDeviceCertificateEventAsync(string prefix, X509Certificate2 certificate, CancellationToken cancellationToken);
    Task SendSignTwinKeyEventAsync(string changeSignKey, CancellationToken cancellationToken);
    Task SendSignFileEventAsync(SignFileEvent d2CMessage, CancellationToken cancellationToken);
    Task SendRemoveDeviceEvent(CancellationToken cancellationToken);
}