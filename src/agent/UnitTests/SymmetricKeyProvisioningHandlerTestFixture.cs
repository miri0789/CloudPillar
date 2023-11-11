using System.Security.Cryptography;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class SymmetricKeyProvisioningHandlerTestFixture
{
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<ISymmetricKeyWrapper> _symmetricKeyWrapperMock;
    private Mock<IProvisioningDeviceClientWrapper> _provisioningDeviceClientWrapperMock;
    private Mock<IOptions<AuthenticationSettings>> _authenticationSettingsMock;
    private ISymmetricKeyProvisioningHandler _target;


    private const string DEVICE_ID = "UnitTest";
    private const string IOT_HUB_HOST_NAME = "IoTHubHostName";
    private const string GENERATION_ID = "generationId";
    const string DPS_SCOPE_ID = "dpsScopeId";
    const string GLOBAL_DEVICE_ENDPOINT = "globalDeviceEndpoint";
    const string GROUP_ENROLLMENT_PRIMARY_KEY = "groupEnrollmentPrimaryKey";
    const string PRIMARY_KEY = "VGVzdEtleQ==";
    const string? SECONDARY_KEY = "";

    AuthenticationSettings authenticationSettings = new AuthenticationSettings()
    {
        DpsScopeId = DPS_SCOPE_ID,
        GlobalDeviceEndpoint = GLOBAL_DEVICE_ENDPOINT,
        GroupEnrollmentKey = GROUP_ENROLLMENT_PRIMARY_KEY,
    };

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _symmetricKeyWrapperMock = new Mock<ISymmetricKeyWrapper>();
        _provisioningDeviceClientWrapperMock = new Mock<IProvisioningDeviceClientWrapper>();
        _authenticationSettingsMock = new Mock<IOptions<AuthenticationSettings>>();
        _authenticationSettingsMock.Setup(x => x.Value).Returns(authenticationSettings);

        _symmetricKeyWrapperMock.Setup(x => x.CreateHMAC(It.IsAny<string>())).Returns(new HMACSHA256(Convert.FromBase64String(PRIMARY_KEY)));
        _symmetricKeyWrapperMock.Setup(x => x.GetSecurityProvider(DEVICE_ID, PRIMARY_KEY, SECONDARY_KEY)).Returns(new SecurityProviderSymmetricKey(DEVICE_ID, PRIMARY_KEY, SECONDARY_KEY));
        _deviceClientWrapperMock.Setup(x => x.GetProvisioningTransportHandler()).Returns(Mock.Of<ProvisioningTransportHandler>());

        _deviceClientWrapperMock.Setup(x => x.IsDeviceInitializedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        CreateTarget();
    }

    [Test]
    public async Task ProvisioningAsync_WithValidParameters_RegistersDevice()
    {
        _authenticationSettingsMock.Setup(x => x.Value).Returns(authenticationSettings);

        _provisioningDeviceClientWrapperMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecurityProvider>(), It.IsAny<ProvisioningTransportHandler>())).ReturnsAsync(() =>
    {
        return GetDeviceRegistrationResult(ProvisioningRegistrationStatusType.Assigned);
    });

        await _target.ProvisioningAsync(DPS_SCOPE_ID, CancellationToken.None);

        _deviceClientWrapperMock.Verify(x => x.DeviceInitializationAsync(It.IsAny<string>(), It.IsAny<DeviceAuthenticationWithX509Certificate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProvisioningAsync_RegisterFaild_RegistersDeviceNotCalled()
    {
        _authenticationSettingsMock.Setup(x => x.Value).Returns(authenticationSettings);

        _provisioningDeviceClientWrapperMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecurityProvider>(), It.IsAny<ProvisioningTransportHandler>())).ReturnsAsync(() =>
        {
            return GetDeviceRegistrationResult(ProvisioningRegistrationStatusType.Failed);
        });

        Assert.ThrowsAsync<Exception>(async () => await _target.ProvisioningAsync(DPS_SCOPE_ID, CancellationToken.None));
    }

    [Test]
    public async Task ProvisioningAsync_InvalidEnviromentValues_ThrowException()
    {
        _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { DpsScopeId = "" });
        CreateTarget();

        Assert.ThrowsAsync<ArgumentException>(async () =>
         await _target.ProvisioningAsync(string.Empty, CancellationToken.None));

    }

    private void CreateTarget()
    {
        _target = new SymmetricKeyProvisioningHandler(_loggerMock.Object, _deviceClientWrapperMock.Object, _symmetricKeyWrapperMock.Object, _provisioningDeviceClientWrapperMock.Object, _authenticationSettingsMock.Object);
    }
    private DeviceRegistrationResult GetDeviceRegistrationResult(ProvisioningRegistrationStatusType statusType)
    {
        return new DeviceRegistrationResult(DEVICE_ID, null, IOT_HUB_HOST_NAME, DEVICE_ID, statusType, GENERATION_ID, null, 0, string.Empty, string.Empty);
    }

}