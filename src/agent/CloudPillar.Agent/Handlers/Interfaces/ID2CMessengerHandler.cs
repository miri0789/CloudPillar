
using System.Security.Cryptography.X509Certificates;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(CancellationToken cancellationToken, string fileName, int actionIndex, int? rangeIndex, long? startPosition = null, long? endPosition = null);
    Task SendStreamingUploadChunkEventAsync(byte[] buffer, Uri storageUri, long currentPosition, string checkSum, CancellationToken cancellationToken, bool isRunDiagnostics = false);
    Task ProvisionDeviceCertificateEventAsync(string prefix, X509Certificate2 certificate, CancellationToken cancellationToken);
    Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey, CancellationToken cancellationToken);
    Task SendSignFileEventAsync(SignFileEvent d2CMessage, CancellationToken cancellationToken);
}