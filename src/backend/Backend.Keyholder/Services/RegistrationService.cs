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

    public async Task Register(string deviceId, string oneMDKey)
    {
        try
        {
            var certificate = GenerateCertificate(deviceId, oneMDKey);
            var enrollment = await CreateEnrollmentAsync(certificate, deviceId);
            await SendCertificateToAgent(deviceId, oneMDKey, certificate, enrollment);
        }
        catch (Exception ex)
        {
            _loggerHandler.Error("Faild to register ", ex);
            throw;
        }
    }



    internal X509Certificate2 GenerateCertificate(string deviceId, string OneMDKey)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(OneMDKey);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        _loggerHandler.Debug($"GenerateCertificate for deviceId {deviceId}");
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{CertificateConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}{deviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(OneMDKey);
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

    internal async Task SendCertificateToAgent(string deviceId, string oneMDKey, X509Certificate2 certificate, IndividualEnrollment individualEnrollment)
    {
        _loggerHandler.Debug($"SendCertificateToAgent for deviceId {deviceId}.");
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(individualEnrollment);
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        ArgumentNullException.ThrowIfNullOrEmpty(oneMDKey);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] passwordBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(oneMDKey));

            string passwordString = BitConverter.ToString(passwordBytes).Replace("-", "").ToLower();

            // The password is temporary and will be fixed in task 11505
            var pfxBytes = certificate.Export(X509ContentType.Pkcs12, "1234");
            var message = new ReProvisioningMessage()
            {
                Data = pfxBytes,
                EnrollmentId = individualEnrollment.RegistrationId,
                DeviceEndpoint = DEVICE_ENDPOINT,
                // TODO: get the scopeid from the dps, not from the env, + do not send the dps connection string
                ScopedId = _environmentsWrapper.dpsIdScope,
                DPSConnectionString = _environmentsWrapper.dpsConnectionString
            };
            var c2dMessage = _messageFactory.PrepareC2DMessage(message);
            await _deviceClientWrapper.SendAsync(_serviceClient, deviceId, c2dMessage);
        }
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