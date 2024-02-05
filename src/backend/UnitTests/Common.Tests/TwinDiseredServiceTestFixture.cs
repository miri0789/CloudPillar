using Moq;
using Shared.Logger;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Services;
using Backend.Infra.Common.Wrappers.Interfaces;
using Shared.Entities.Twin;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Utilities;


namespace Backend.Infra.Common.tests;
public class TwinDiseredServiceTestFixture
{
    private ITwinDiseredService _target;
    private Mock<IRegistryManagerWrapper> _registryManagerWrapperMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    
    [SetUp]
    public void SetUp()
    {
        _registryManagerWrapperMock = new Mock<IRegistryManagerWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();

        _registryManagerWrapperMock.Setup(x => x.CreateFromConnectionString()).Returns(new RegistryManager());
        _target = new TwinDiseredService(_loggerHandlerMock.Object, _registryManagerWrapperMock.Object);
    }

    [Test]
    public async Task AddDesiredRecipeAsync_NoChangeSpecId_SetId()
    {
        var desired = new TwinDesired()
        {
            ChangeSpec = new Dictionary<string, TwinChangeSpec>()
            {
                {
                SharedConstants.CHANGE_SPEC_NAME, new TwinChangeSpec() {
                        Id =   null
                    }
                }
            }
        };


        var reported = new TwinReported();
        var twin = MockHelperBackend.CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);
        _target.AddDesiredRecipeAsync("deviceId", SharedConstants.CHANGE_SPEC_NAME, new DownloadAction());

        TwinDesired twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();

        _registryManagerWrapperMock.Verify(x => x.UpdateTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>(),
         It.Is<Twin>(c => twinDesired.GetDesiredChangeSpecByKey(SharedConstants.CHANGE_SPEC_NAME).Id.Contains(SharedConstants.CHANGE_SPEC_NAME.ToString())), It.IsAny<string>()), Times.Once);
    }
}

