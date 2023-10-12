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
using Backend.Keyholder.Wrappers.Interfaces;
using Shared.Entities.Authentication;
using Newtonsoft.Json;

public class RegistrationService : IRegistrationService
{
    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    private const int KEY_SIZE_IN_BITS = 4096;
    private const string DEVICE_ENDPOINT = "global.azure-devices-provisioning.net";
    private readonly ILoggerHandler _loggerHandler;

    private readonly IMessageFactory _messageFactory;

    private readonly IDeviceClientWrapper _deviceClientWrapper;

    private readonly ServiceClient _serviceClient;

    private readonly IEnvironmentsWrapper _environmentsWrapper;

    private readonly IIndividualEnrollmentWrapper _individualEnrollmentWrapper;
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly string _iotHubHostName;

    public RegistrationService(
        IMessageFactory messageFactory,
        IDeviceClientWrapper deviceClientWrapper,
        IEnvironmentsWrapper environmentsWrapper,
        IIndividualEnrollmentWrapper individualEnrollmentWrapper,
        IX509CertificateWrapper x509CertificateWrapper,
        ILoggerHandler loggerHandler)
    {
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _loggerHandler = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _individualEnrollmentWrapper = individualEnrollmentWrapper ?? throw new ArgumentNullException(nameof(individualEnrollmentWrapper));
        _x509CertificateWrapper = x509CertificateWrapper ?? throw new ArgumentNullException(nameof(x509CertificateWrapper));
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.iothubConnectionString);
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.dpsConnectionString);

        _serviceClient = _deviceClientWrapper.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
        _iotHubHostName = GetIOTHubHostName();
    }



    public async Task Register(string deviceId, string secretKey)
    {
        try
        {
            _loggerHandler.Debug($"SendCertificateToAgent for deviceId {deviceId}.");
            ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
            ArgumentNullException.ThrowIfNullOrEmpty(secretKey);
            var authonticationKeys = JsonConvert.SerializeObject(new AuthonticationKeys()
            {
                DeviceId = deviceId,
                SecretKey = secretKey
            });

            var message = new RequestDeviceCertificateMessage()
            {
                Data = Encoding.ASCII.GetBytes(authonticationKeys)
            };

            var c2dMessage = _messageFactory.PrepareC2DMessage(message);
            await _deviceClientWrapper.SendAsync(_serviceClient, deviceId, c2dMessage);
            // var certificate = GenerateCertificate(deviceId, oneMDKey);
            // var enrollment = await CreateEnrollmentAsync(certificate, deviceId);
            // await SendCertificateToAgent(deviceId, oneMDKey, certificate, enrollment);
        }
        catch (Exception ex)
        {
            _loggerHandler.Error("Faild to register ", ex);
            throw;
        }
    }

    public async Task ProvisionDeviceCertificate(string deviceId, byte[] certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        var cert = new X509Certificate2(certificate);
        await CreateEnrollmentAsync(cert, deviceId);



    }

    internal async Task<IndividualEnrollment> CreateEnrollmentAsync(X509Certificate2 certificate, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        _loggerHandler.Debug($"CreateEnrollmentAsync for deviceId {deviceId}");
        var enrollmentName = CertificateConstants.CLOUD_PILLAR_SUBJECT + deviceId;
        using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(_environmentsWrapper.dpsConnectionString))
        {
            var cer = _x509CertificateWrapper.CreateCertificate(certificate.Export(X509ContentType.Cert));
            Attestation attestation = X509Attestation.CreateFromClientCertificates(cer);

            try
            {
                await provisioningServiceClient.DeleteIndividualEnrollmentAsync(enrollmentName);
                _loggerHandler.Debug($"Individual enrollment for deviceId {deviceId} was deleted");
            }
            catch (ProvisioningServiceClientException ex)
            {
                //If the enrollment does not exist, it throws an exception when attempting to delete it.
            }

            var individualEnrollment = _individualEnrollmentWrapper.Create(enrollmentName, attestation);

            individualEnrollment.ProvisioningStatus = ProvisioningStatus.Enabled;
            individualEnrollment.DeviceId = deviceId;
            individualEnrollment.IotHubHostName = _iotHubHostName;

            return await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);

        }
    }

    internal async Task SendReprovisioningMessageToAgent(string deviceId, IndividualEnrollment individualEnrollment)
    {
        _loggerHandler.Debug($"SendReprovisioningMessageToAgent for deviceId {deviceId}.");
        ArgumentNullException.ThrowIfNull(individualEnrollment);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        var message = new ReprovisioningMessage()
        {
            Data = Encoding.ASCII.GetBytes(individualEnrollment.RegistrationId),
            DeviceEndpoint = DEVICE_ENDPOINT,
            // TODO: get the scopeid from the dps, not from the env, + do not send the dps connection string
            ScopedId = _environmentsWrapper.dpsIdScope,
            DPSConnectionString = _environmentsWrapper.dpsConnectionString
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