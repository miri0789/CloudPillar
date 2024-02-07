using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Entities.Authentication;
using Shared.Entities.Messages;
using CloudPillar.Agent.Handlers.Logger;

namespace CloudPillar.Agent.Handlers;
public class ReprovisioningHandler : IReprovisioningHandler
{
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly ISHA256Wrapper _sHA256Wrapper;
    private readonly IProvisioningServiceClientWrapper _provisioningServiceClientWrapper;
    private readonly IX509Provider _x509Provider;
    private readonly AuthenticationSettings _authenticationSettings;
    private readonly ILoggerHandler _logger;


    public ReprovisioningHandler(
        IX509CertificateWrapper X509CertificateWrapper,
        IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
        ID2CMessengerHandler d2CMessengerHandler,
        ISHA256Wrapper sHA256Wrapper,
        IProvisioningServiceClientWrapper provisioningServiceClientWrapper,
        IOptions<AuthenticationSettings> options,
        IX509Provider x509Provider,
        ILoggerHandler logger)
    {
        _x509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _sHA256Wrapper = sHA256Wrapper ?? throw new ArgumentNullException(nameof(sHA256Wrapper));
        _provisioningServiceClientWrapper = provisioningServiceClientWrapper ?? throw new ArgumentNullException(nameof(provisioningServiceClientWrapper));
        _x509Provider = x509Provider ?? throw new ArgumentNullException(nameof(x509Provider));
        _authenticationSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task HandleReprovisioningMessageAsync(Message recivedMessage, ReprovisioningMessage message, CancellationToken cancellationToken)
    {
        ValidateReprovisioningMessage(message);

        var certificate = GetTempCertificate();
        ArgumentNullException.ThrowIfNull(certificate);

        var deviceId = certificate.Subject.Replace($"{ProvisioningConstants.CERTIFICATE_SUBJECT}{_authenticationSettings.GetCertificatePrefix()}", string.Empty);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);

        string iotHubHostName = await GetIotHubHostNameAsync(message.DPSConnectionString, message.Data, cancellationToken);
        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);

        await ProvisionDeviceAndCleanupCertificatesAsync(recivedMessage, message, certificate, deviceId, iotHubHostName, cancellationToken);
    }

    public async Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var data = JsonConvert.DeserializeObject<AuthenticationKeys>(Encoding.Unicode.GetString(message.Data));
        ArgumentNullException.ThrowIfNull(data);
        var certificate = _x509Provider.GenerateCertificate(data.DeviceId, data.SecretKey, _authenticationSettings.CertificateExpiredDays);
        ArgumentNullException.ThrowIfNull(certificate);
        InstallTemporaryCertificate(certificate, data.SecretKey);
        await _d2CMessengerHandler.ProvisionDeviceCertificateEventAsync(_authenticationSettings.GetCertificatePrefix(), certificate, cancellationToken);
    }

    private void ValidateReprovisioningMessage(ReprovisioningMessage message)
    {
        if (message == null || message.Data == null)
        {
            throw new ArgumentNullException("Invalid message.");
        }
    }

    private async Task<string> GetIotHubHostNameAsync(string dpsConnectionString, byte[] data, CancellationToken cancellationToken)
    {
        var enrollment = await _provisioningServiceClientWrapper.GetIndividualEnrollmentAsync(dpsConnectionString, Encoding.Unicode.GetString(data), cancellationToken);
        return enrollment.IotHubHostName;
    }

    private async Task ProvisionDeviceAndCleanupCertificatesAsync(Message recivedMessage, ReprovisioningMessage message, X509Certificate2 certificate, string deviceId, string iotHubHostName, CancellationToken cancellationToken)
    {
        try
        {
            var isProvisioningSuccess = await _dPSProvisioningDeviceClientHandler.ProvisioningAsync(message.ScopedId, certificate, message.DeviceEndpoint, recivedMessage, cancellationToken);
            if (isProvisioningSuccess)
            {
                throw new ArgumentNullException("Provisioning failed");
            }
            using (var store = _x509CertificateWrapper.Open(OpenFlags.ReadWrite, storeLocation: _authenticationSettings.StoreLocation))
            {
                CleanCertificates(store, certificate, deviceId, iotHubHostName);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Provisioning failed", ex);
            throw;
        }

    }

    private void CleanCertificates(X509Store store, X509Certificate2 certificate, string deviceId, string iotHubHostName)
    {
        X509Certificate2Collection certificates = _x509CertificateWrapper.GetCertificates(store);
        if (certificates == null)
        {
            throw new ArgumentNullException("certificates", "Certificates collection cannot be null.");
        }

        RemoveCertificatesFromStore(store, certificate.Thumbprint);

        X509Certificate2Collection collection = _x509CertificateWrapper.Find(store, X509FindType.FindByThumbprint, certificate.Thumbprint, false);
        if (collection.Count == 0)
        {
            throw new ArgumentNullException("certificates", "Certificates collection must contains temporary cert.");
        }

        X509Certificate2 cert = collection[0];
        cert.FriendlyName = $"{deviceId}{ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR}{iotHubHostName.Replace(ProvisioningConstants.IOT_HUB_NAME_SUFFIX, string.Empty)}";
    }

    private void InstallTemporaryCertificate(X509Certificate2 certificate, string secretKey)
    {

        byte[] passwordBytes = _sHA256Wrapper.ComputeHash(Encoding.UTF8.GetBytes(secretKey));

        string passwordString = BitConverter.ToString(passwordBytes).Replace("-", "").ToLower();

        var pfxBytes = certificate.Export(X509ContentType.Pkcs12, passwordString);

        var privateCertificate = _x509CertificateWrapper.CreateFromBytes(pfxBytes, passwordString, X509KeyStorageFlags.PersistKeySet);

        privateCertificate.FriendlyName = _authenticationSettings.GetTemporaryCertificate();

        using (var store = _x509CertificateWrapper.Open(OpenFlags.ReadWrite, storeLocation: _authenticationSettings.StoreLocation))
        {
            RemoveCertificatesFromStore(store, string.Empty);
            _x509CertificateWrapper.Add(store, privateCertificate);
        }
    }

    private X509Certificate2? GetTempCertificate()
    {
        using (var store = _x509CertificateWrapper.Open(OpenFlags.ReadOnly, storeLocation: _authenticationSettings.StoreLocation))
        {
            X509Certificate2Collection certificates = _x509CertificateWrapper.GetCertificates(store);

            var filteredCertificate = certificates?.Cast<X509Certificate2>()
               .Where(cert => cert.FriendlyName == _authenticationSettings.GetTemporaryCertificate())
               .FirstOrDefault();

            if (filteredCertificate == null)
            {
                _logger.Info("GetTempCertificate not find certificate");
            }
            return filteredCertificate;
        }
    }

    public void RemoveX509CertificatesFromStore()
    {
        using (var store = _x509CertificateWrapper.Open(OpenFlags.ReadWrite, storeLocation: _authenticationSettings.StoreLocation))
        {
            RemoveCertificatesFromStore(store, string.Empty);
        }
    }

    private void RemoveCertificatesFromStore(X509Store store, string thumbprint)
    {
        var certificates = _x509CertificateWrapper.GetCertificates(store);

        var filteredCertificates = certificates?.Cast<X509Certificate2>()
           .Where(cert => string.IsNullOrEmpty(thumbprint) && cert.FriendlyName == _authenticationSettings.GetTemporaryCertificate() ||
            (cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + _authenticationSettings.GetCertificatePrefix())
           && cert.Thumbprint != thumbprint))
           .ToArray();

        if (filteredCertificates?.Length > 0)
        {
            var certificateCollection = _x509CertificateWrapper.CreateCertificateCollecation(filteredCertificates);
            _x509CertificateWrapper.RemoveRange(store, certificateCollection);
        }
    }
}