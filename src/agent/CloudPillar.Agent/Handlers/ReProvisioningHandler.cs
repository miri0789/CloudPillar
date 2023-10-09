using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
using Shared.Entities.Messages;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class ReProvisioningHandler : IReProvisioningHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;

    private readonly IX509CertificateWrapper _x509CertificateWrapper;

    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;

    private readonly ILoggerHandler _logger;

    private const char CERTIFICATE_NAME_SEPARATOR = '@';
    private const string IOT_HUB_NAME_SUFFIX = ".azure-devices.net";

    public ReProvisioningHandler(IDeviceClientWrapper deviceClientWrapper,
        IX509CertificateWrapper X509CertificateWrapper,
             IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
              ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _x509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }
    public async Task HandleReProvisioningMessageAsync(ReProvisioningMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var certificateBytes = message.Data;
        X509Certificate2 certificate = new X509Certificate2(certificateBytes, "1234");


        var deviceId = certificate.Subject.Replace(CertificateConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT, string.Empty); ;
        // var secretKey = string.Empty;
        // foreach (X509Extension extension in certificate.Extensions)
        // {

        //     if (extension.Oid?.Value == CertificateConstants.ONE_MD_EXTENTION_KEY)
        //     {
        //         secretKey = Encoding.UTF8.GetString(extension.RawData);
        //     }
        // }

        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        // ArgumentNullException.ThrowIfNullOrEmpty(secretKey);



        var iotHubHostName = string.Empty;
        using (ProvisioningServiceClient provisioningServiceClient =
                           ProvisioningServiceClient.CreateFromConnectionString(message.DPSConnectionString))
        {
            var enrollment = await provisioningServiceClient.GetIndividualEnrollmentAsync(message.EnrollmentId);
            iotHubHostName = enrollment.IotHubHostName;
        }

        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);

        certificate.FriendlyName = $"{deviceId}{CERTIFICATE_NAME_SEPARATOR}{iotHubHostName.Replace(IOT_HUB_NAME_SUFFIX, string.Empty)}";


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
}