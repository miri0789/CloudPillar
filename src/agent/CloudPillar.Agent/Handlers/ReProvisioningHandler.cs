using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public class ReProvisioningHandler : IReProvisioningHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IX509CertificateWrapper _x509CertificateWrapper;

    public ReProvisioningHandler(IDeviceClientWrapper deviceClientWrapper,
        IX509CertificateWrapper X509CertificateWrapper)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _x509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
    }
    public async Task HandleReProvisioningMessageAsync(ReProvisioningMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        
        var certificateBytes = message.Data;
        X509Certificate2 certificate = new X509Certificate2(certificateBytes, "1234");

        var iotHubHostName = string.Empty;
        using (ProvisioningServiceClient provisioningServiceClient =
                           ProvisioningServiceClient.CreateFromConnectionString(message.DPSConnectionString))
        {
            var enrollment = await provisioningServiceClient.GetIndividualEnrollmentAsync(message.EnrollmentId);
            iotHubHostName = enrollment.IotHubHostName;
        }

         ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);


        //  var certificateString = Convert.ToBase64String(certificateBytes);



        // Create an X509Store object for the certificate store you want to install it in
        using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite); // Open the store with write access

            certificate.FriendlyName = $"key_holder@{iotHubHostName.Split(".")[0]}";
            // Add the certificate to the store
            store.Add(certificate);
            //    store.Certificates.Add(certificate);

            Console.WriteLine("Certificate installed successfully.");
        }
    }
}