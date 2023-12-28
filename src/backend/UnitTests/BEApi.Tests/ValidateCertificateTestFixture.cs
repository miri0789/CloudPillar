using Backend.BEApi.Services;
using Backend.BEApi.Services.Interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Azure.Devices;
using Moq;
using Shared.Logger;
using Shared.Entities.Twin;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Backend.BEApi.Tests;

public class ValidateCertificateTestFixture
{
    private Mock<IRegistrationService> _registrationServiceMock;
    private Mock<IRegistryManagerWrapper> _registryManagerWrapperMock;
    private Mock<IEnvironmentsWrapper> _environmentsWrapperMock;
    private Mock<ILoggerHandler> _loggerMock;
    private IValidateCertificateService _target;
    private const string DEVICE_ID = "deviceId";
    private const string SECRET_KEY = "secretKey";
    private const string CERTIFICATE_PREFIX = "UnitTest-CP-";

    [SetUp]
    public void Setup()
    {
        _registrationServiceMock = new Mock<IRegistrationService>();
        _registryManagerWrapperMock = new Mock<IRegistryManagerWrapper>();
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _loggerMock = new Mock<ILoggerHandler>();
        _environmentsWrapperMock.Setup(c => c.dpsConnectionString).Returns("dpsConnectionString");
        _environmentsWrapperMock.Setup(c => c.iothubConnectionString).Returns("HostName=unitTest;SharedAccessKeyName=iothubowner;");
        _environmentsWrapperMock.Setup(c => c.expirationCertificatePercent).Returns(0.6);

        _target = new ValidateCertificateService(_registrationServiceMock.Object,
         _registryManagerWrapperMock.Object,
          _environmentsWrapperMock.Object,
           _loggerMock.Object);
    }

    [Test]
    public async Task IsDevicesCertificateExpiredAsync_ExpiredCertificates_MessageSendToAgent()
    {
        var device = new Device(DEVICE_ID);
        var twin = new Twin();
        var twinReported = new TwinReported();
        twinReported.CertificateValidity = new CertificateValidity();
        twinReported.CertificateValidity.CreationDate = DateTime.UtcNow.AddDays(-2);
        twinReported.CertificateValidity.ExpirationDate = DateTime.UtcNow.AddDays(1);
        twinReported.SecretKey = SECRET_KEY;
        string reportedJson = JsonConvert.SerializeObject(twinReported);
        twin.Properties.Reported = JsonConvert.DeserializeObject<TwinCollection>(reportedJson);

        var devices = new List<Device>();
        devices.Add(device);
        _registryManagerWrapperMock.Setup(x => x.GetIotDevicesAsync(It.IsAny<RegistryManager>(), It.IsAny<int>())).ReturnsAsync(devices);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);
        await _target.IsDevicesCertificateExpiredAsync();
        _registrationServiceMock.Verify(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task IsDevicesCertificateExpiredAsync_ValidParameters_MessageNotSendToAgent()
    {
        var device = new Device(DEVICE_ID);
        var twin = new Twin();
        var twinReported = new TwinReported();
        twinReported.CertificateValidity = new CertificateValidity();
        twinReported.CertificateValidity.CreationDate = DateTime.UtcNow.AddDays(-1);
        twinReported.CertificateValidity.ExpirationDate = DateTime.UtcNow.AddDays(10);
        twinReported.SecretKey = SECRET_KEY;
        string reportedJson = JsonConvert.SerializeObject(twinReported);
        twin.Properties.Reported = JsonConvert.DeserializeObject<TwinCollection>(reportedJson);
        var devices = new List<Device>();
        devices.Add(device);
        _registryManagerWrapperMock.Setup(x => x.GetIotDevicesAsync(It.IsAny<RegistryManager>(), It.IsAny<int>())).ReturnsAsync(devices);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);
        await _target.IsDevicesCertificateExpiredAsync();
        _registrationServiceMock.Verify(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}