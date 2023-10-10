using System.Security.Cryptography;
using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Shared.Logger;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client;


namespace CloudPillar.Agent.Tests;

[TestFixture]
public class SymmetricKeyProvisioningDeviceClientHandlerTestFixure
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<ISymmetricKeyWrapper> _symmetricKeyWrapperMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private ISymmetricKeyProvisioningDeviceClientHandler _target;


    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _symmetricKeyWrapperMock = new Mock<ISymmetricKeyWrapper>();
        _target = new SymmetricKeyProvisioningDeviceClientHandler(_loggerHandlerMock.Object, _deviceClientMock.Object, _symmetricKeyWrapperMock.Object);

        var _securityMock = new Mock<SecurityProviderSymmetricKey>();
        _symmetricKeyWrapperMock.Setup(f => f.GetSecurityProvider(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .Returns(() => _securityMock);

        var _deviceAuthentication = new Mock<DeviceAuthenticationWithRegistrySymmetricKey>();
        _symmetricKeyWrapperMock.Setup(f => f.GetDeviceAuthentication(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(() => _deviceAuthentication);
    }



    [Test]
    public async Task ProvisionWithSymmetricKeyAsync_ValidData_NotThrowing()
    {
        await _target.ProvisionWithSymmetricKeyAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>());

        _deviceClientMock.Verify(dc => dc.IsDeviceInitializedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}