using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
using Newtonsoft.Json;
using Shared.Entities.Authentication;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class ReprovisioningHandler : IReprovisioningHandler
{

    private readonly IX509CertificateWrapper _x509CertificateWrapper;

    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly ILoggerHandler _logger;

    private const int KEY_SIZE_IN_BITS = 4096;

    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    private const string TEMPORARY_CERTIFICATE_NAME = "CPTemporaryCertificate";

    public ReprovisioningHandler(IX509CertificateWrapper X509CertificateWrapper,
        IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
        IEnvironmentsWrapper environmentsWrapper,
        ID2CMessengerHandler d2CMessengerHandler,
        ILoggerHandler logger
        )
    {
        _x509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }
    public async Task HandleReprovisioningMessageAsync(ReprovisioningMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Data);

        var certificate = GetTempCertificate();
        ArgumentNullException.ThrowIfNull(certificate);

        var deviceId = certificate.Subject.Replace($"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}", string.Empty);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);

        var iotHubHostName = string.Empty;
        using (ProvisioningServiceClient provisioningServiceClient =
                           ProvisioningServiceClient.CreateFromConnectionString(message.DPSConnectionString))
        {
            var enrollment = await provisioningServiceClient.GetIndividualEnrollmentAsync(Encoding.Unicode.GetString(message.Data), cancellationToken);
            iotHubHostName = enrollment.IotHubHostName;
        }

        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);

        await _dPSProvisioningDeviceClientHandler.ProvisioningAsync(message.ScopedId, certificate, message.DeviceEndpoint, cancellationToken);
        using (X509Store store = _x509CertificateWrapper.GetStore(StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);

            X509Certificate2Collection certificates = store.Certificates;
            if (certificates == null)
            {
                throw new ArgumentNullException("certificates", "Certificates collection cannot be null.");
            }

            var filteredCertificates = certificates.Cast<X509Certificate2>()
               .Where(cert => cert.Subject.StartsWith($"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}")
               && cert.Thumbprint != certificate.Thumbprint)
               .ToArray();
            if (filteredCertificates?.Length > 0)
            {
                var certificateCollection = new X509Certificate2Collection(filteredCertificates);
                store.RemoveRange(certificateCollection);
            }

            X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
            if (collection.Count == 0)
            {
                throw new ArgumentNullException("certificates", "Certificates collection must contains temporary cert.");
            }

            X509Certificate2 cert = collection[0];

            cert.FriendlyName = $"{deviceId}{ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR}{iotHubHostName.Replace(ProvisioningConstants.IOT_HUB_NAME_SUFFIX, string.Empty)}";

            store.Close();

        }


    }

    public async Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var data = JsonConvert.DeserializeObject<AuthonticationKeys>(Encoding.Unicode.GetString(message.Data));
        ArgumentNullException.ThrowIfNull(data);
        var certificate = GenerateCertificate(message, data);
        InstallTemporaryCertificate(certificate, data.SecretKey);
        await _d2CMessengerHandler.ProvisionDeviceCertificateEventAsync(certificate);
    }



    private X509Certificate2 GenerateCertificate(RequestDeviceCertificateMessage message, AuthonticationKeys data)
    {
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}{data.DeviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(data.SecretKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid(ProvisioningConstants.ONE_MD_EXTENTION_KEY, ONE_MD_EXTENTION_NAME),
                oneMDKeyValue, false
               );


            request.CertificateExtensions.Add(OneMDKeyExtension);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(_environmentsWrapper.certificateExpiredDays));

            return certificate;

        }
    }

    private void InstallTemporaryCertificate(X509Certificate2 certificate, string secretKey)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] passwordBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));

            string passwordString = BitConverter.ToString(passwordBytes).Replace("-", "").ToLower();

            var pfxBytes = certificate.Export(X509ContentType.Pkcs12, passwordString);

            var privateCertificate = new X509Certificate2(pfxBytes, passwordString);

            privateCertificate.FriendlyName = TEMPORARY_CERTIFICATE_NAME;


            using (X509Store store = _x509CertificateWrapper.GetStore(StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);

                X509Certificate2Collection certificates = store.Certificates;
                if (certificates != null)
                {

                    var filteredCertificates = certificates.Cast<X509Certificate2>()
                       .Where(cert => cert.FriendlyName == TEMPORARY_CERTIFICATE_NAME)
                       .ToArray();
                    if (filteredCertificates?.Length > 0)
                    {
                        var certificateCollection = new X509Certificate2Collection(filteredCertificates);
                        store.RemoveRange(certificateCollection);
                    }
                }

                store.Add(privateCertificate);
            }
        }

    }

    private X509Certificate2 GetTempCertificate()
    {

        using (X509Store store = _x509CertificateWrapper.GetStore(StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = store.Certificates;
            var filteredCertificate = certificates?.Cast<X509Certificate2>()
               .Where(cert => cert.FriendlyName == TEMPORARY_CERTIFICATE_NAME)
               .FirstOrDefault();

            if (filteredCertificate == null)
            {
                _logger.Info("GetTempCertificate not find certificate");
            }
            return filteredCertificate;
        }

    }
}