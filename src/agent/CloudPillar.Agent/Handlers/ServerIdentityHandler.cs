
using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Twin;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace CloudPillar.Agent.Handlers;

public class ServerIdentityHandler : IServerIdentityHandler
{
    private readonly ILoggerHandler _logger;
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly IDeviceClientWrapper _deviceClient;
    private const string CERTFICATE_FILE_EXTENSION = "*.cer";
    private const string PUBLIC_KEY_FILE_EXTENSION = ".pem";

    public ServerIdentityHandler(
        ILoggerHandler loggerHandler,
        IX509CertificateWrapper x509CertificateWrapper,
        IFileStreamerWrapper fileStreamerWrapper,
        IDeviceClientWrapper deviceClient
)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _x509CertificateWrapper = x509CertificateWrapper ?? throw new ArgumentNullException(nameof(x509CertificateWrapper));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
    }


    public async Task HandleKnownIdentitiesFromCertificatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            string[] certificatesFiles = _fileStreamerWrapper.GetFiles(Constants.PKI_FOLDER_PATH, CERTFICATE_FILE_EXTENSION);
            if (certificatesFiles.Length == 0)
            {
                _logger.Info($"No certificates found in {Constants.PKI_FOLDER_PATH}");
                return;
            }
            var knownIdentitiesList = GetKnownIdentitiesByCertFiles(certificatesFiles, cancellationToken);
            await UpdateKnownIdentitiesInReportedAsync(knownIdentitiesList, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"HandleKnownIdentitiesFromCertificatesAsync failed message: {ex.Message}");
            throw new Exception(ex.Message);
        }

    }

    public async Task<string> GetPublicKeyFromCertificateFileAsync(string certificatePath)
    {
        X509Certificate2 certificate = _x509CertificateWrapper.CreateFromFile(certificatePath);
        ECDsa publicKey = _x509CertificateWrapper.GetECDsaPublicKey(certificate);
        if (publicKey == null)
        {
            throw new Exception($"GetPublicKeyFromCertificateFileAsync failed to get public key from certificate {certificatePath}");
        }
        string pemPublicKey = ConvertToPem(publicKey);
        return pemPublicKey;
    }

    private string ConvertToPem(ECDsa publicKey)
    {
        byte[] base64Key = _x509CertificateWrapper.ExportSubjectPublicKeyInfo(publicKey);
        string pem = $"-----BEGIN PUBLIC KEY-----\n";
        pem += Convert.ToBase64String(base64Key, Base64FormattingOptions.InsertLineBreaks);
        pem += $"\n-----END PUBLIC KEY-----\n";
        return pem;
    }

    private List<KnownIdentities> GetKnownIdentitiesByCertFiles(string[] certificatesFiles, CancellationToken cancellationToken)
    {
        var knownIdentitiesList = certificatesFiles
                        .Select(certificatePath =>
                        {
                            X509Certificate2 cert = _x509CertificateWrapper.CreateFromFile(certificatePath);
                            return new KnownIdentities(
                            cert.Subject,
                            cert.Thumbprint,
                            $"{cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss")}"
                            );
                        }).ToList();
        return knownIdentitiesList;
    }

    private async Task UpdateKnownIdentitiesInReportedAsync(List<KnownIdentities> knownIdentitiesList, CancellationToken cancellationToken)
    {
        var key = nameof(TwinReported.KnownIdentities);
        await _deviceClient.UpdateReportedPropertiesAsync(key, knownIdentitiesList, cancellationToken);
        _logger.Info($"UpdateKnownIdentitiesInReported success");
    }
}