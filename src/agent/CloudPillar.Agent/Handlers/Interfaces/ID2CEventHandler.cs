
using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(CancellationToken cancellationToken, string fileName, string actionId, int? rangeIndex, long? startPosition = null, long? endPosition = null);
    Task SendStreamingUploadChunkEventAsync(byte[] buffer, Uri storageUri, string actionId, long currentPosition, string checkSum, CancellationToken cancellationToken);
    Task ProvisionDeviceCertificateEventAsync(X509Certificate2 certificate, CancellationToken cancellationToken);
}