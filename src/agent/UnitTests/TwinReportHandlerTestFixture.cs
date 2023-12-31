using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Shared;
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
        CreateTwinMock(new TwinChangeSpec(), new TwinReportedChangeSpec(), existingCustomProps);
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


    private void CreateTwinMock(TwinChangeSpec twinChangeSpec, TwinReportedChangeSpec twinReportedChangeSpec, List<TwinReportedCustomProp>? twinReportedCustomProps = null, string? changeSign = "----")
    {
        var twin = MockHelper.CreateTwinMock(twinChangeSpec, twinReportedChangeSpec, null, null, twinReportedCustomProps, changeSign);
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
        CreateTwinMock(new TwinChangeSpec(), new TwinReportedChangeSpec());
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
        CreateTwinMock(new TwinChangeSpec(), new TwinReportedChangeSpec(), existingCustomProps);
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
        var actionsToReported = new List<ActionToReport> { new ActionToReport
        {
            ReportPartName = "InstallSteps",
            ReportIndex = 0,
            TwinReport = new TwinActionReported { Status = StatusType.InProgress }
        } };

        CreateTwinMock(new TwinChangeSpec(), new TwinReportedChangeSpec()
        {
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>() { new TwinActionReported() { } }.ToArray()
            }
        });

        return actionsToReported;
    }

}