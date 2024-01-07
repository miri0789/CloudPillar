
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
        string[] certificatesFiles = _fileStreamerWrapper.GetFiles(Constants.PKI_FOLDER_PATH, CERTFICATE_FILE_EXTENSION);
        await UpdateKnownIdentitiesByCertFilesAsync(certificatesFiles, true, cancellationToken);
    }

    public async Task UpdateKnownIdentitiesByCertFilesAsync(string[] certificatesFiles, bool initList, CancellationToken cancellationToken)
    {
        try
        {
            var knownIdentitiesList = new List<KnownIdentities>();

            foreach (string certificatePath in certificatesFiles)
            {
                if (!_fileStreamerWrapper.FileExists(certificatePath))
                {
                    _logger.Error($"UpdateKnownIdentitiesByCertFiles failed, certificate file not exist in path: {certificatePath}");
                    throw new FileNotFoundException($"{certificatePath} not found");
                }
                X509Certificate2 cert = _x509CertificateWrapper.CreateFromFile(certificatePath);
                var knownIdentity = new KnownIdentities(cert.Subject, cert.Thumbprint,
                 $"{cert.NotAfter.ToShortDateString()} {cert.NotAfter.ToShortTimeString()}");

                knownIdentitiesList.Add(knownIdentity);
            }
            await UpdateKnownIdentitiesInReportedAsync(knownIdentitiesList, initList, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateKnownIdentitiesByCertFiles failed message: {ex.Message}");
        }
    }

    public async Task<string> GetPublicKeyFromCertificate(X509Certificate2 certificate)
    {
        RSA publicKey = _x509CertificateWrapper.GetRSAPublicKey(certificate);
        string pemPublicKey = ConvertToPem(publicKey);
        return pemPublicKey;
    }

    private string ConvertToPem(RSA publicKey)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("-----BEGIN PUBLIC KEY-----");

        string base64Key = _x509CertificateWrapper.ExportSubjectPublicKeyInfo(publicKey);
        int offset = 0;
        while (offset < base64Key.Length)
        {
            int lineLength = Math.Min(64, base64Key.Length - offset);
            builder.AppendLine(base64Key.Substring(offset, lineLength));
            offset += lineLength;
        }

        builder.AppendLine("-----END PUBLIC KEY-----");
        return builder.ToString();
    }

    private async Task UpdateKnownIdentitiesInReportedAsync(List<KnownIdentities> knownIdentitiesList, bool initList, CancellationToken cancellationToken)
    {
        try
        {
            List<KnownIdentities> knownIdentitiesReported = null;
            if (knownIdentitiesList?.Count > 0)
            {
                if (initList)
                {
                    knownIdentitiesReported = new List<KnownIdentities>();
                }
                else
                {
                    var twin = await _deviceClient.GetTwinAsync(cancellationToken);
                    string reportedJson = twin.Properties.Reported.ToJson();
                    var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson);
                    knownIdentitiesReported = twinReported?.KnownIdentities ?? new List<KnownIdentities>();
                }

                knownIdentitiesReported.AddRange(knownIdentitiesList);

                var key = nameof(TwinReported.KnownIdentities);
                await _deviceClient.UpdateReportedPropertiesAsync(key, knownIdentitiesReported, cancellationToken);
                _logger.Info($"UpdateKnownIdentitiesInReported success");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceCustomPropsAsync failed message: {ex.Message}");
        }
    }
}