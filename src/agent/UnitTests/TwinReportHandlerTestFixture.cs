using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Moq;
using Newtonsoft.Json.Linq;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using System.Runtime.InteropServices;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class TwinReportHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<IRuntimeInformationWrapper> _runtimeInformationWrapperMock;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private ITwinReportHandler _target;
    private const string CHANGE_SPEC_ID = "123";
    private const string PATCH_KEY = "TransitPackage";

    private CancellationToken cancellationToken = CancellationToken.None;

    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _runtimeInformationWrapperMock = new Mock<IRuntimeInformationWrapper>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();

        _target = new TwinReportHandler(_deviceClientMock.Object, _loggerHandlerMock.Object,
         _runtimeInformationWrapperMock.Object,
         _fileStreamerWrapperMock.Object);

        CreateTarget();
    }

    private void CreateTarget()
    {
        _target = new TwinReportHandler(_deviceClientMock.Object, _loggerHandlerMock.Object,
                _runtimeInformationWrapperMock.Object,
                _fileStreamerWrapperMock.Object);
    }

    [Test]
    public async Task GetPeriodicReportedKey_OnExec_ReturnKeys()
    {
        PeriodicUploadAction periodicUploadAction = new PeriodicUploadAction()
        {
            DirName = "agent"
        };
        var key = _target.GetPeriodicReportedKey(periodicUploadAction, "agent\\Cloud Pillar.Agent");
        Assert.AreEqual("cloud%20pillar_agent", key);
    }

    [Test]
    public async Task GetPeriodicReportedKey_OnExecWithEnd_ReturnKeys()
    {
        PeriodicUploadAction periodicUploadAction = new PeriodicUploadAction()
        {
            DirName = "agent\\"
        };
        var key = _target.GetPeriodicReportedKey(periodicUploadAction, "agent\\Cloud Pillar.Agent");
        Assert.AreEqual("cloud%20pillar_agent", key);
    }

    [Test]
    public async Task GetActionToReport_OnExec_ReturnActionToReport()
    {
        var key = "cloud%20pillar_agent";
        var actionToReport = new ActionToReport()
        {
            TwinAction = new PeriodicUploadAction()
            {
                DirName = "agent"
            },
            TwinReport = new TwinActionReported()
            {
                PeriodicReported = new Dictionary<string, TwinActionReported>()
                {
                    { key, new TwinActionReported() { Status = StatusType.InProgress } }
                }
            }
        };
        var actionToReported = _target.GetActionToReport(actionToReport, "agent\\Cloud Pillar.Agent");
        Assert.AreEqual(StatusType.InProgress, actionToReported.Status);
    }

    [Test]
    public async Task GetActionToReport_OnTwinActionNotPeriodic_ReturnActionToReport()
    {
        var key = "cloud%20pillar_agent";
        var actionToReport = new ActionToReport()
        {
            TwinAction = new TwinAction()
        };
        var actionToReported = _target.GetActionToReport(actionToReport, "agent\\Cloud Pillar.Agent");
        Assert.AreEqual(null, actionToReported.Status);
    }

    [Test]
    public async Task SetReportProperties_OnExec_UpdateReport()
    {
        var actionToReport = new ActionToReport()
        {
            TwinAction = new PeriodicUploadAction()
            {
                DirName = "agent"
            },
            TwinReport = new TwinActionReported()
            {
                PeriodicReported = new Dictionary<string, TwinActionReported>()
                {
                    { "cloud%20pillar_agent", new TwinActionReported() { Status = StatusType.InProgress } }
                }
            }
        };
        _target.SetReportProperties(actionToReport, StatusType.Success);
        Assert.AreEqual(StatusType.InProgress, actionToReport.TwinReport.PeriodicReported["cloud%20pillar_agent"].Status);
    }

    [Test]
    public async Task UpdateReportActionAsync_ValidReport_CallToUpdateReport()
    {
        var actionsToReported = CreateReportForUpdating();

        await _target.UpdateReportActionAsync(actionsToReported, cancellationToken);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(
            nameof(TwinReported.ChangeSpec), It.IsAny<JObject>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceCustomPropsAsync_ExistingProps_OverrideProps()
    {
        var existingCustomProps = new List<TwinReportedCustomProp>
        {
            new TwinReportedCustomProp { Name = "Property1", Value = "Value1" },
            new TwinReportedCustomProp { Name = "Property2", Value = "Value2" }
        };
        CreateTwinMock(new Dictionary<string, TwinChangeSpec>(), new Dictionary<string, TwinReportedChangeSpec>(), existingCustomProps);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        var newCustomProps = new List<TwinReportedCustomProp>
        {
            new TwinReportedCustomProp { Name = "Property2", Value = "NewValue2" },
            new TwinReportedCustomProp { Name = "Property3", Value = "Value3" }
        };

        await _target.UpdateDeviceCustomPropsAsync(newCustomProps, cancellationToken);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.Custom), It.Is<List<TwinReportedCustomProp>>(
            props => props.Count == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceStateAfterServiceRestartAsync_ValidState_Success()
    {
        var deviceState = DeviceStateType.Busy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.UpdateDeviceStateAfterServiceRestartAsync(deviceState, default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.DeviceStateAfterServiceRestart), deviceState.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }


    private void CreateTwinMock(Dictionary<string, TwinChangeSpec> twinChangeSpec, Dictionary<string, TwinReportedChangeSpec> twinReportedChangeSpec, List<TwinReportedCustomProp>? twinReportedCustomProps = null, Dictionary<string, string>? changeSign = null)
    {
        var twin = MockHelper.CreateTwinMock(twinChangeSpec, twinReportedChangeSpec, twinReportedCustomProps, changeSign);
        _deviceClientMock.Setup(dc => dc.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);

    }

    [Test]
    public async Task InitReportDeviceParamsAsync_OnUpdateReportFailed_LogFailure()
    {
        var expectedErrorMessage = "Simulated error message";
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception(expectedErrorMessage));

        await _target.InitReportDeviceParamsAsync(default);
        _loggerHandlerMock.Verify(logger => logger.Error($"InitReportedDeviceParams failed: {expectedErrorMessage}"), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceCustomPropsAsync_CustomPropsNull_UpdateNotExecute()
    {
        CreateTwinMock(new Dictionary<string, TwinChangeSpec>(), new Dictionary<string, TwinReportedChangeSpec>());
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.UpdateDeviceCustomPropsAsync(null, cancellationToken);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.Custom), It.IsAny<List<TwinReportedCustomProp>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateDeviceCustomPropsAsync_NewProps_AddProps()
    {
        var existingCustomProps = new List<TwinReportedCustomProp>
        {
            new TwinReportedCustomProp { Name = "Property1", Value = "Value1" },
            new TwinReportedCustomProp { Name = "Property2", Value = "Value2" }
        };
        CreateTwinMock(new Dictionary<string, TwinChangeSpec>(), new Dictionary<string, TwinReportedChangeSpec>(), existingCustomProps);

        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        var newCustomProps = new List<TwinReportedCustomProp>
        {
            new TwinReportedCustomProp { Name = "Property4", Value = "Value4" },
            new TwinReportedCustomProp { Name = "Property3", Value = "Value3" }
        };

        await _target.UpdateDeviceCustomPropsAsync(newCustomProps, cancellationToken);


        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.Custom), It.Is<List<TwinReportedCustomProp>>(
            props => props.Count == 4), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceStateAsync_ValidState_Success()
    {
        var deviceState = DeviceStateType.Busy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.UpdateDeviceStateAsync(deviceState, default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.DeviceState), deviceState.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceStateAsync_OnUpdateReportFailed_LogFailure()
    {
        var expectedErrorMessage = "my error";
        var deviceState = DeviceStateType.Busy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), deviceState.ToString(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception(expectedErrorMessage));
        await _target.UpdateDeviceStateAsync(deviceState, default);
        _loggerHandlerMock.Verify(logger => logger.Error($"UpdateDeviceStateAsync failed: {expectedErrorMessage}"), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_OnExec_UpdateOSDescription()
    {
        var agentPlatformKey = nameof(TwinReported.AgentPlatform);
        var osDescription = "Mocked OS Description";
        _runtimeInformationWrapperMock.Setup(dc => dc.GetOSDescription()).Returns(osDescription);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(agentPlatformKey, osDescription, It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync(default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(agentPlatformKey, osDescription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_WindowsSupportedShells_UpdateWindowsSupportedShells()
    {
        var supportedShellsKey = nameof(TwinReported.SupportedShells);
        _runtimeInformationWrapperMock.Setup(dc => dc.IsOSPlatform(OSPlatform.Windows)).Returns(true);
        _fileStreamerWrapperMock.Setup(dc => dc.FileExists(It.IsAny<string>())).Returns(true);

        await _target.InitReportDeviceParamsAsync(default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey,
        new List<ShellType>() { ShellType.Cmd, ShellType.Powershell, ShellType.Bash }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_LinuxSupportedShells_UpdateLinuxSupportedShells()
    {
        var supportedShellsKey = nameof(TwinReported.SupportedShells);
        _runtimeInformationWrapperMock.Setup(dc => dc.IsOSPlatform(OSPlatform.Linux)).Returns(true);
        _runtimeInformationWrapperMock.Setup(dc => dc.IsOSPlatform(OSPlatform.Windows)).Returns(false);
        _fileStreamerWrapperMock.Setup(dc => dc.FileExists(It.IsAny<string>())).Returns(true);

        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey, It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync(default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey,
        new List<ShellType>() { ShellType.Bash, ShellType.Powershell }, It.IsAny<CancellationToken>()), Times.Once);
    }



    private List<ActionToReport> CreateReportForUpdating()
    {
        var actionsToReported = new List<ActionToReport> { new ActionToReport()
        {
            ReportPartName = "TransitPackage",
            ReportIndex = 0,
            TwinReport = new TwinActionReported { Status = StatusType.InProgress }
        } };

        var reported = new Dictionary<string, TwinReportedChangeSpec>
            {
                {
                    TwinConstants.CHANGE_SPEC_NAME, new TwinReportedChangeSpec()
                    {
                        Id =  CHANGE_SPEC_ID,
                        Patch = new Dictionary<string, TwinActionReported[]>()
                        {
                            { PATCH_KEY, new TwinActionReported[0] }
                        }
                    }
                }
            };

        CreateTwinMock(new Dictionary<string, TwinChangeSpec>(), reported);

        return actionsToReported;
    }

}