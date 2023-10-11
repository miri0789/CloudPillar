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
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class ReProvisioningHandler : IReProvisioningHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;

    private readonly IX509CertificateWrapper _x509CertificateWrapper;

    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly ILoggerHandler _logger;

    private const int KEY_SIZE_IN_BITS = 4096;

    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    private const string TEMPORARY_CERTIFICATE_NAME = "temporaryCertificate";

    public ReProvisioningHandler(IDeviceClientWrapper deviceClientWrapper,
        IX509CertificateWrapper X509CertificateWrapper,
        IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
        IEnvironmentsWrapper environmentsWrapper,
        ID2CMessengerHandler d2CMessengerHandler,
        ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _x509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }
    public async Task HandleReProvisioningMessageAsync(ReProvisioningMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var certificateBytes = message.Data;
        // The password is temporary and will be fixed in task 11505
        X509Certificate2 certificate = new X509Certificate2(certificateBytes, "1234");


        var deviceId = certificate.Subject.Replace(CertificateConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT, string.Empty); ;


        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);



        var iotHubHostName = string.Empty;
        using (ProvisioningServiceClient provisioningServiceClient =
                           ProvisioningServiceClient.CreateFromConnectionString(message.DPSConnectionString))
        {
            var enrollment = await provisioningServiceClient.GetIndividualEnrollmentAsync(message.EnrollmentId);
            iotHubHostName = enrollment.IotHubHostName;
        }

        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);

        certificate.FriendlyName = $"{deviceId}{CertificateConstants.CERTIFICATE_NAME_SEPARATOR}{iotHubHostName.Replace(CertificateConstants.IOT_HUB_NAME_SUFFIX, string.Empty)}";


        using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);

            X509Certificate2Collection certificates = store.Certificates;
            if (certificates != null)
            {

                var filteredCertificates = certificates.Cast<X509Certificate2>()
                   .Where(cert => cert.Subject.StartsWith(CertificateConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT))
                   .ToArray();
                if (filteredCertificates != null && filteredCertificates.Length > 0)
                {
                    var certificateCollection = new X509Certificate2Collection(filteredCertificates);
                    store.RemoveRange(certificateCollection);
                }
            }

            store.Add(certificate);
        }

        try
        {
            await _dPSProvisioningDeviceClientHandler.ProvisioningAsync(message.ScopedId, certificate, message.DeviceEndpoint, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Provisioning failed", ex);
            throw;
        }

    }

    public async Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var data = JsonConvert.DeserializeObject<AuthonticationKeys>(Encoding.ASCII.GetString(message.Data));
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
                $"{CertificateConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}{data.DeviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(data.SecretKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid(CertificateConstants.ONE_MD_EXTENTION_KEY, ONE_MD_EXTENTION_NAME),
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

            certificate.FriendlyName = TEMPORARY_CERTIFICATE_NAME;


        }

        using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);

            X509Certificate2Collection certificates = store.Certificates;
            if (certificates != null)
            {

                var filteredCertificates = certificates.Cast<X509Certificate2>()
                   .Where(cert => cert.FriendlyName == TEMPORARY_CERTIFICATE_NAME)
                   .ToArray();
                if (filteredCertificates != null && filteredCertificates.Length > 0)
                {
                    var certificateCollection = new X509Certificate2Collection(filteredCertificates);
                    store.RemoveRange(certificateCollection);
                }
            }

            store.Add(certificate);
        }

    }
}