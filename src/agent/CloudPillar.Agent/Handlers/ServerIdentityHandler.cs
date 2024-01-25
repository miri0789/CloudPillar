
using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Twin;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;

public class ServerIdentityHandler : IServerIdentityHandler
{
    private readonly ILoggerHandler _logger;
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly IDeviceClientWrapper _deviceClient;
    private AppSettings _appSettings;
    private const string CERTFICATE_FILE_EXTENSION = "*.cer";
    private const string PUBLIC_KEY_FILE_EXTENSION = ".pem";

    public ServerIdentityHandler(
        ILoggerHandler loggerHandler,
        IX509CertificateWrapper x509CertificateWrapper,
        IFileStreamerWrapper fileStreamerWrapper,
        IDeviceClientWrapper deviceClient,
        IOptions<AppSettings> appSettings
)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _x509CertificateWrapper = x509CertificateWrapper ?? throw new ArgumentNullException(nameof(x509CertificateWrapper));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }


    public async Task UpdateKnownIdentitiesFromCertificatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            string[] certificatesFiles = _fileStreamerWrapper.GetFiles(Constants.PKI_FOLDER_PATH, CERTFICATE_FILE_EXTENSION);
            var knownIdentitiesList = GetKnownIdentitiesByCertFiles(certificatesFiles, cancellationToken);
            await UpdateKnownIdentitiesInReportedAsync(knownIdentitiesList, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateKnownIdentitiesFromCertificatesAsync failed message: {ex.Message}");
            throw new Exception(ex.Message);
        }

    }

    public async Task<string> GetPublicKeyFromCertificateFileAsync(string certificatePath)
    {
        X509Certificate2 certificate = _x509CertificateWrapper.CreateFromFile(certificatePath);
        byte[] publicKey = _x509CertificateWrapper.ExportSubjectPublicKeyInfo(certificate);
        if (publicKey == null)
        {
            throw new Exception($"GetPublicKeyFromCertificateFileAsync failed to get public key from certificate {certificatePath}");
        }
        string pemPublicKey = ConvertToPem(publicKey, certificate.SignatureAlgorithm.FriendlyName?.ToUpper());
        return pemPublicKey;
    }

    public async Task RemoveNonDefaultCertificates(string path)
    {
        try
        {
            _logger.Info($"Start removing non default certificates from path: {path}");

            List<string> certificatesFiles = _fileStreamerWrapper.GetFiles(path, CERTFICATE_FILE_EXTENSION).ToList();
            certificatesFiles.ForEach(certificateFile =>
            {
                if (_fileStreamerWrapper.GetFileNameWithoutExtension(certificateFile).ToLower() != _appSettings.DefaultPublicKeyName.ToLower())
                {
                    _fileStreamerWrapper.DeleteFile(certificateFile);
                    _logger.Info($"RemoveNonDefaultCertificates success for file: {certificateFile}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"RemoveNonDefaultCertificates failed message: {ex.Message}");
        }
    }

    private string ConvertToPem(byte[] base64Key, string? keyAlgorithm)
    {
        string algo = keyAlgorithm!.Contains("RSA") ? "RSA" : "";
        StringBuilder pemBuilder = new StringBuilder();
        pemBuilder.AppendLine($"-----BEGIN {algo} PUBLIC KEY-----");
        pemBuilder.AppendLine(Convert.ToBase64String(base64Key, Base64FormattingOptions.InsertLineBreaks));
        pemBuilder.AppendLine($"-----END {algo} PUBLIC KEY-----");
        return pemBuilder.ToString();
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