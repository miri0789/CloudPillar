using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
using Newtonsoft.Json;
using Shared.Entities.Authentication;
using Shared.Entities.Messages;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class ReprovisioningHandler : IReprovisioningHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;

    private readonly IX509CertificateWrapper _x509CertificateWrapper;

    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly ISHA256Wrapper _sHA256Wrapper;
    private readonly IProvisioningServiceClientWrapper _provisioningServiceClientWrapper;
    private readonly ILoggerHandler _logger;

    public ReprovisioningHandler(IDeviceClientWrapper deviceClientWrapper,
        IX509CertificateWrapper X509CertificateWrapper,
        IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
        IEnvironmentsWrapper environmentsWrapper,
        ID2CMessengerHandler d2CMessengerHandler,
        ISHA256Wrapper sHA256Wrapper,
        IProvisioningServiceClientWrapper provisioningServiceClientWrapper,
        ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _x509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _sHA256Wrapper = sHA256Wrapper ?? throw new ArgumentNullException(nameof(sHA256Wrapper));
        _provisioningServiceClientWrapper = provisioningServiceClientWrapper ?? throw new ArgumentNullException(nameof(provisioningServiceClientWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }
    public async Task HandleReprovisioningMessageAsync(ReprovisioningMessage message, CancellationToken cancellationToken)
    {
        ValidateReprovisioningMessage(message);

        var certificate = GetTempCertificate();
        ArgumentNullException.ThrowIfNull(certificate);

        var deviceId = certificate.Subject.Replace($"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}", string.Empty);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);

        string iotHubHostName = await GetIotHubHostNameAsync(message.DPSConnectionString, message.Data, cancellationToken);
        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);

        await ProvisionDeviceAndCleanupCertificatesAsync(message, certificate, deviceId, iotHubHostName, cancellationToken);
    }

    public async Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var data = JsonConvert.DeserializeObject<AuthonticationKeys>(Encoding.Unicode.GetString(message.Data));
        ArgumentNullException.ThrowIfNull(data);
        var certificate = X509Provider.GenerateCertificate(data.DeviceId, data.SecretKey, _environmentsWrapper.certificateExpiredDays);
        InstallTemporaryCertificate(certificate, data.SecretKey);
        await _d2CMessengerHandler.ProvisionDeviceCertificateEventAsync(certificate);
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


    private async Task ProvisionDeviceAndCleanupCertificatesAsync(ReprovisioningMessage message, X509Certificate2 certificate, string deviceId, string iotHubHostName, CancellationToken cancellationToken)
    {
        try
        {
            await _dPSProvisioningDeviceClientHandler.ProvisioningAsync(message.ScopedId, certificate, message.DeviceEndpoint, cancellationToken);
            _x509CertificateWrapper.Open(OpenFlags.ReadWrite);

            X509Certificate2Collection certificates = _x509CertificateWrapper.Certificates;
            if (certificates != null)
            {

                var filteredCertificates = certificates.Cast<X509Certificate2>()
                   .Where(cert => cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT)
                   && cert.Thumbprint != certificate.Thumbprint)
                   .ToArray();

                if (filteredCertificates != null && filteredCertificates.Length > 0)
                {
                    var certificateCollection = _x509CertificateWrapper.CreateCertificateCollecation(filteredCertificates);
                    _x509CertificateWrapper.RemoveRange(certificateCollection);
                }

                X509Certificate2Collection collection = _x509CertificateWrapper.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);

                if (collection.Count > 0)
                {
                    X509Certificate2 cert = collection[0];
                    cert.FriendlyName = $"{deviceId}{ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR}{iotHubHostName.Replace(ProvisioningConstants.IOT_HUB_NAME_SUFFIX, string.Empty)}";
                }

                _x509CertificateWrapper.Close();

            }


        }
        catch (Exception ex)
        {
            _logger.Error($"Provisioning failed", ex);
            throw;
        }

    }

    private void InstallTemporaryCertificate(X509Certificate2 certificate, string secretKey)
    {

        byte[] passwordBytes = _sHA256Wrapper.ComputeHash(Encoding.UTF8.GetBytes(secretKey));

        string passwordString = BitConverter.ToString(passwordBytes).Replace("-", "").ToLower();

        var pfxBytes = certificate.Export(X509ContentType.Pkcs12, passwordString);

        var privateCertificate = _x509CertificateWrapper.CreateFromBytes(pfxBytes, passwordString);

        privateCertificate.FriendlyName = CertificateConstants.TEMPORARY_CERTIFICATE_NAME;

        _x509CertificateWrapper.Open(OpenFlags.ReadWrite);
        var certificates = _x509CertificateWrapper.Certificates;

        if (certificates != null)
        {

            var filteredCertificates = certificates.Cast<X509Certificate2>()
               .Where(cert => cert.FriendlyName == CertificateConstants.TEMPORARY_CERTIFICATE_NAME)
               .ToArray();

            if (filteredCertificates != null && filteredCertificates.Length > 0)
            {
                var certificateCollection = _x509CertificateWrapper.CreateCertificateCollecation(filteredCertificates);
                _x509CertificateWrapper.RemoveRange(certificateCollection);
            }
        }

        _x509CertificateWrapper.Add(privateCertificate);
        _x509CertificateWrapper.Close();
    }

    private X509Certificate2 GetTempCertificate()
    {
        _x509CertificateWrapper.Open(OpenFlags.ReadOnly);
        X509Certificate2Collection certificates = _x509CertificateWrapper.Certificates;
        if (certificates == null)
        {
            return null;
        }
        var filteredCertificate = certificates.Cast<X509Certificate2>()
           .Where(cert => cert.FriendlyName == CertificateConstants.TEMPORARY_CERTIFICATE_NAME)
           .FirstOrDefault();

        return filteredCertificate;
    }
}