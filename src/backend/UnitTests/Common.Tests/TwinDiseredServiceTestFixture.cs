using Moq;
using Shared.Logger;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Services;
using Backend.Infra.Common.Wrappers.Interfaces;
using Shared.Entities.Twin;
using Microsoft.Azure.Devices;

namespace Backend.Infra.Common.tests;
public class TwinDiseredServiceTestFixture
{
    private ITwinDiseredService _target;
    private Mock<IRegistryManagerWrapper> _registryManagerWrapperMock;
    private Mock<IGuidWrapper> _guidWrapperMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;

    [SetUp]
    public void SetUp()
    {
        _registryManagerWrapperMock = new Mock<IRegistryManagerWrapper>();
        _guidWrapperMock = new Mock<IGuidWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();

        _registryManagerWrapperMock.Setup(x => x.CreateFromConnectionString()).Returns(new RegistryManager());
        _target = new TwinDiseredService(_loggerHandlerMock.Object, _registryManagerWrapperMock.Object, _guidWrapperMock.Object);
    }

    [Test]
    public async Task AddDesiredRecipeAsync_NoChangeSpenId_SetId()
    {

        var desired = new TwinDesired()
        {
            ChangeSpec = new TwinChangeSpec()
            {
                Id = null
            }
        };

        var reported = new TwinReported();
        var twin = MockHelperBackend.CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);
        _target.AddDesiredRecipeAsync("deviceId", TwinPatchChangeSpec.ChangeSpec, new DownloadAction());

        _guidWrapperMock.Verify(x => x.NewGuid(), Times.Once);
    }
}

