using Microsoft.Azure.Devices.Provisioning.Service;
using Shared.Logger;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Shared.Entities.Messages;
using Shared.Entities.Factories;
using Backend.Infra.Common;
using Microsoft.Azure.Devices;
using Backend.Keyholder.Wrappers.Interfaces;
using Shared.Entities.Authentication;
using Newtonsoft.Json;
using Backend.Infra.Common;

public class RegistrationService : IRegistrationService
{
    private readonly ILoggerHandler _loggerHandler;

    private readonly IMessageFactory _messageFactory;

    private readonly IDeviceConnectService _deviceConnectService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IIndividualEnrollmentWrapper _individualEnrollmentWrapper;
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly IProvisioningServiceClientWrapper _provisioningServiceClientWrapper;
    private readonly string _iotHubHostName;

    public RegistrationService(
        IMessageFactory messageFactory,
        IDeviceConnectService deviceConnectService,
        IEnvironmentsWrapper environmentsWrapper,
        IIndividualEnrollmentWrapper individualEnrollmentWrapper,
        IX509CertificateWrapper x509CertificateWrapper,
        IProvisioningServiceClientWrapper provisioningServiceClientWrapper,
        ILoggerHandler loggerHandler)
    {
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _deviceConnectService = deviceConnectService ?? throw new ArgumentNullException(nameof(deviceConnectService));
        _loggerHandler = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _individualEnrollmentWrapper = individualEnrollmentWrapper ?? throw new ArgumentNullException(nameof(individualEnrollmentWrapper));
        _x509CertificateWrapper = x509CertificateWrapper ?? throw new ArgumentNullException(nameof(x509CertificateWrapper));
        _provisioningServiceClientWrapper = provisioningServiceClientWrapper ?? throw new ArgumentNullException(nameof(provisioningServiceClientWrapper));
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.iothubConnectionString);
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.dpsConnectionString);

        _iotHubHostName = GetIOTHubHostName();
    }



    public async Task RegisterAsync(string deviceId, string secretKey)
    {
        try
        {
            _loggerHandler.Debug($"Register for deviceId {deviceId}.");
            ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
            ArgumentNullException.ThrowIfNullOrEmpty(secretKey);
            var authenticationKeys = JsonConvert.SerializeObject(new AuthenticationKeys()
            {
                DeviceId = deviceId,
                SecretKey = secretKey
            });

            var message = new RequestDeviceCertificateMessage()
            {
                Data = Encoding.Unicode.GetBytes(authenticationKeys)
            };

            var c2dMessage = _messageFactory.PrepareC2DMessage(message);

            await _deviceConnectService.SendDeviceMessageAsync(c2dMessage, deviceId);
        }
        catch (Exception ex)
        {
            _loggerHandler.Error("Faild to register ", ex);
            throw;
        }
    }

    public async Task ProvisionDeviceCertificateAsync(string deviceId, byte[] certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentException.ThrowIfNullOrEmpty(deviceId);
        _loggerHandler.Debug($"ProvisionDeviceCertificate for deviceId {deviceId}.");
        try
        {
            var cert = _x509CertificateWrapper.CreateCertificate(certificate);
            var enrollment = await CreateEnrollmentAsync(cert, deviceId);
            await SendReprovisioningMessageToAgentAsync(deviceId, enrollment);
        }
        catch (Exception ex)
        {
            _loggerHandler.Error("ProvisionDeviceCertificate ", ex);
            throw;
        }
    }

    private async Task<IndividualEnrollment> CreateEnrollmentAsync(X509Certificate2 certificate, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        _loggerHandler.Debug($"CreateEnrollmentAsync for deviceId {deviceId}");
        var enrollmentName = CertificateConstants.CLOUD_PILLAR_SUBJECT + deviceId;
        var provisioningServiceClient = _provisioningServiceClientWrapper.Create(_environmentsWrapper.dpsConnectionString);


        var cer = _x509CertificateWrapper.CreateCertificate(certificate.Export(X509ContentType.Cert));
        Attestation attestation = X509Attestation.CreateFromClientCertificates(cer);

        try
        {
            await _provisioningServiceClientWrapper.DeleteIndividualEnrollmentAsync(provisioningServiceClient, enrollmentName);
            _loggerHandler.Debug($"Individual enrollment for deviceId {deviceId} was deleted");
        }
        catch (ProvisioningServiceClientException ex)
        {
            //If the enrollment does not exist, it throws an exception when attempting to delete it.
            _loggerHandler.Debug($"There is no individual enrollment for deviceId {deviceId} for deleted");
        }

        var individualEnrollment = _individualEnrollmentWrapper.Create(enrollmentName, attestation);

        individualEnrollment.ProvisioningStatus = ProvisioningStatus.Enabled;
        individualEnrollment.DeviceId = deviceId;
        individualEnrollment.IotHubHostName = _iotHubHostName;
        individualEnrollment.ReprovisionPolicy = new ReprovisionPolicy() { MigrateDeviceData = true, UpdateHubAssignment = true };

        return await _provisioningServiceClientWrapper.CreateOrUpdateIndividualEnrollmentAsync(provisioningServiceClient, individualEnrollment);


    }

    private async Task SendReprovisioningMessageToAgentAsync(string deviceId, IndividualEnrollment individualEnrollment)
    {
        _loggerHandler.Debug($"SendReprovisioningMessageToAgent for deviceId {deviceId}.");
        ArgumentNullException.ThrowIfNull(individualEnrollment);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        var message = new ReprovisioningMessage()
        {
            Data = Encoding.Unicode.GetBytes(individualEnrollment.RegistrationId),
            DeviceEndpoint = _environmentsWrapper.globalDeviceEndpoint,
            // TODO: get the scopeid from the dps, not from the env, + do not send the dps connection string
            ScopedId = _environmentsWrapper.dpsIdScope,
            DPSConnectionString = _environmentsWrapper.dpsConnectionString
        };
        var c2dMessage = _messageFactory.PrepareC2DMessage(message);


        await _deviceConnectService.SendDeviceMessageAsync(c2dMessage, deviceId);

    }

    private string GetIOTHubHostName()
    {

        string iotHubHostName = _environmentsWrapper.iothubConnectionString
        .Split(';')
        .Select(part => part.Trim())
        .FirstOrDefault(part => part.StartsWith("HostName=", StringComparison.OrdinalIgnoreCase))
        ?.Substring("HostName=".Length);

        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);
        return iotHubHostName;
    }
}