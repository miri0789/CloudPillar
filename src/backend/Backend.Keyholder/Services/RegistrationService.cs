using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Shared.Logger;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Shared.Entities.Messages;
using Shared.Entities.Factories;
using Shared.Entities.DeviceClient;
using Microsoft.Azure.Devices;
using Backend.Keyholder.Interfaces;

public class RegistrationService : IRegistrationService
{
    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    private const string HOST_NAME_EXTENTION_NAME = "iotHubHostName";
    private const int KEY_SIZE_IN_BITS = 4096;
    private readonly ILoggerHandler _loggerHandler;

    private readonly IMessageFactory _messageFactory;

    private readonly IDeviceClientWrapper _deviceClientWrapper;

    private readonly ServiceClient _serviceClient;

    private readonly IEnvironmentsWrapper _environmentsWrapper;

    private readonly string _iotHubHostName;

    public RegistrationService(IMessageFactory messageFactory, IDeviceClientWrapper deviceClientWrapper, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler loggerHandler)
    {
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _loggerHandler = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.iothubConnectionString);
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.dpsConnectionString);

        _serviceClient = _deviceClientWrapper.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
        _iotHubHostName = GetIOTHubHostName();
    }

    public async Task Register(string deviceId, string OneMDKey, string password)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        ArgumentNullException.ThrowIfNullOrEmpty(OneMDKey);

        var certificate = GenerateCertificate(deviceId, OneMDKey);
        await CreateEnrollmentAsync(certificate, deviceId);
        await SendCertificateToAgent(certificate, password);
    }



    internal X509Certificate2 GenerateCertificate(string deviceId, string OneMDKey)
    {
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{CertificateConstants.CLOUD_PILLAR_SUBJECT}{deviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(OneMDKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid(CertificateConstants.ONE_MD_EXTENTION_KEY, ONE_MD_EXTENTION_NAME),
                oneMDKeyValue, false
               );

            byte[] iotHubHostNameValue = Encoding.UTF8.GetBytes(_iotHubHostName);
            var iotHubHostNameExtension = new X509Extension(
                new Oid(CertificateConstants.IOT_HUB_HOST_NAME_EXTENTION_KEY, HOST_NAME_EXTENTION_NAME),
                iotHubHostNameValue, false
               );

            request.CertificateExtensions.Add(OneMDKeyExtension);
            request.CertificateExtensions.Add(iotHubHostNameExtension);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(30));

            return certificate;


        }
    }

    internal async Task CreateEnrollmentAsync(X509Certificate2 certificate, string deviceId)
    {
        using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(_environmentsWrapper.dpsConnectionString))
        {

            try
            {
                var cer = new X509Certificate2(certificate.Export(X509ContentType.Cert));
                Attestation attestation = X509Attestation.CreateFromClientCertificates(cer);

                var individualEnrollment = new IndividualEnrollment(deviceId, attestation)
                {
                    ProvisioningStatus = ProvisioningStatus.Enabled,
                    DeviceId = deviceId,
                    IotHubHostName = _iotHubHostName
                };

                var result = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                _loggerHandler.Error("asdasda", ex);
            }
        }
    }

    internal async Task SendCertificateToAgent(X509Certificate2 certificate, string password)
    {
        var pfxBytes = certificate.Export(X509ContentType.Pkcs12, password);
        string certificateBase64 = Convert.ToBase64String(pfxBytes);
        var deviceId = "YourDeviceId"; // Replace with the device ID of the target device.
        var message = new RegisterCertificateMessage()
        {
            Certificate = certificateBase64,
            Password = password
        };
        var c2dMessage = _messageFactory.PrepareC2DMessage(message);
        await _deviceClientWrapper.SendAsync(_serviceClient, deviceId, c2dMessage);
    }

    private string GetIOTHubHostName()
    {
        string[] parts = _environmentsWrapper.iothubConnectionString.Split(';');

        string iotHubHostName = string.Empty;

        foreach (var part in parts)
        {
            if (part.StartsWith("HostName="))
            {
                iotHubHostName = part.Substring("HostName=".Length);
                break;
            }
        }

        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);
        return iotHubHostName;
    }
}