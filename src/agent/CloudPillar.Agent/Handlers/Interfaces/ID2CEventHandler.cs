
using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, string actionId/*, TwinPatchChangeSpec changeSpecKey*/, long? startPosition = null, long? endPosition = null);
    Task SendStreamingUploadChunkEventAsync(byte[] buffer, Uri storageUri, string actionId, long currentPosition, string checkSum, bool fromRunDiagnostic);
    Task ProvisionDeviceCertificateEventAsync(X509Certificate2 certificate);
}