using Moq;
using Shared.Logger;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Services;
using Backend.Infra.Common.Wrappers.Interfaces;
using Shared.Entities.Twin;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Utilities;
using System.Text;
using Newtonsoft.Json;


namespace Backend.Infra.Common.tests;
public class TwinDiseredServiceTestFixture
{
    private ITwinDiseredService _target;
    private Mock<IRegistryManagerWrapper> _registryManagerWrapperMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private object assignChangeSpec;
    private TwinDesired desired;
    private TwinDesired desiredChangeSpec;

    [SetUp]
    public void SetUp()
    {
        _registryManagerWrapperMock = new Mock<IRegistryManagerWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        assignChangeSpec = "{\"id\":\"try\",\"patch\":{\"transist2\":[{\"source\":\"tryDownloadShort.txt\",\"destinationPath\":\"C:/cp/tryDownloadShort.txt\",\"unzip\":false,\"action\":2,\"sign\":\"ALXnXdOQOIwYKBQavYWRnF0XHOJVtQRyZxkJo1ViGEFiWb/01cVMXcrUmt4QSfjn7kINGUxYbK+L/lIw6KJ/xIXzAOG3JouHgbxFHT72pW3CjTc0RFMYZfjqLuTBGyFfCkX0Pb2xuqHuYXr6qPA2tUmLwBc4jDOeRcRMdeMrsIFHfuNh\",\"description\":\"deny\"}]}}";
        SetupDesireds();

        _registryManagerWrapperMock.Setup(x => x.CreateFromConnectionString()).Returns(new RegistryManager());
        _target = new TwinDiseredService(_loggerHandlerMock.Object, _registryManagerWrapperMock.Object);
    }

    private void SetupDesireds()
    {
        desired = new TwinDesired()
        {
            ChangeSpec = new Dictionary<string, TwinChangeSpec>()
            {
                {
                TwinConstants.CHANGE_SPEC_NAME, new TwinChangeSpec() {
                        Id =   null
                    }
                }
            }
        };

        desiredChangeSpec = new TwinDesired()
        {
            ChangeSpec = new Dictionary<string, TwinChangeSpec>()
            {
                {
                TwinConstants.CHANGE_SPEC_NAME, new TwinChangeSpec() {
                        Id = "try",
                        Patch = new Dictionary<string, TwinAction[]>()
                    }
                }
            }
        };
    }

    [Test]
    public async Task AddDesiredRecipeAsync_NoChangeSpecId_SetId()
    {
        var reported = new TwinReported();
        var twin = MockHelperBackend.CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);
        _target.AddDesiredRecipeAsync("deviceId", TwinConstants.CHANGE_SPEC_NAME, new DownloadAction());

        TwinDesired twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();

        _registryManagerWrapperMock.Verify(x => x.UpdateTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>(),
         It.Is<Twin>(c => twinDesired.GetDesiredChangeSpecByKey(TwinConstants.CHANGE_SPEC_NAME).Id.Contains(TwinConstants.CHANGE_SPEC_NAME.ToString())), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task AddChangeSpec_NoChangeSpecKey_AddChangeSpec()
    {
        var reported = new TwinReported();
        var twin = MockHelperBackend.CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);

        var twinDesired = await _target.AddChangeSpec("deviceId", TwinConstants.CHANGE_SPEC_NAME, assignChangeSpec);
        Assert.AreEqual(twinDesired.ChangeSpec.Count(), 1);
    }

    [Test]
    public async Task AddChangeSpec_OnChangeSpecKeyExists_AddChangeSpec()
    {

        var reported = new TwinReported();
        var twin = MockHelperBackend.CreateTwinMock(desiredChangeSpec.ChangeSpec, reported.ChangeSpec);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);

        var twinDesired = await _target.AddChangeSpec("deviceId", TwinConstants.CHANGE_SPEC_NAME, assignChangeSpec);
        Assert.AreEqual(twinDesired.ChangeSpec.Count(), 1);
    }

    [Test]
    public async Task GetTwinDesiredDataToSign_OnCall_ReturnBytse()
    {
        var changeSpec = desiredChangeSpec.GetDesiredChangeSpecByKey(TwinConstants.CHANGE_SPEC_NAME);
        var dataToSign = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(changeSpec));
        Assert.AreEqual(_target.GetTwinDesiredDataToSign(desiredChangeSpec, TwinConstants.CHANGE_SPEC_NAME), dataToSign);
    }

    [Test]
    public async Task GetTwinDesiredAsync_OnCall_ReturnTwinDesired()
    {
        var reported = new TwinReported();
        var twin = MockHelperBackend.CreateTwinMock(desiredChangeSpec.ChangeSpec, reported.ChangeSpec);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);

        var twinDesired = await _target.GetTwinDesiredAsync("deviceId");
        Assert.AreEqual(twinDesired.ChangeSpec.Count(), 1);
    }

    [Test]
    public async Task GetTwinDesiredAsync_OnNoDeviceId_ThrowExeption()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.GetTwinDesiredAsync(null));
    }

    [Test]
    public async Task GetTwinDesiredAsync_OnNoTwinCall_ThrowExeption()
    {
        var res = await _target.GetTwinDesiredAsync("deviceId");
        Assert.IsNull(res);
    }

    [Test]
    public async Task SignTwinDesiredAsync_OnCall_ReturnTwinDesired()
    {
        var reported = new TwinReported();
        var twin = MockHelperBackend.CreateTwinMock(desiredChangeSpec.ChangeSpec, reported.ChangeSpec);
        _registryManagerWrapperMock.Setup(x => x.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>())).ReturnsAsync(twin);

        await _target.SignTwinDesiredAsync(desiredChangeSpec, "deviceId", TwinConstants.CHANGE_SPEC_NAME, "signature");
        _registryManagerWrapperMock.Verify(x => x.UpdateTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>(), It.IsAny<Twin>(), It.IsAny<string>()), Times.Once);
    }
}