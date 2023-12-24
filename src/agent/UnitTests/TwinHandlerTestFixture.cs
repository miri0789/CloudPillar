using System.Runtime.InteropServices;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class TwinHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<IFileDownloadHandler> _fileDownloadHandlerMock;
    private Mock<IFileUploaderHandler> _fileUploaderHandlerMock;
    private Mock<ITwinActionsHandler> _twinActionsHandler;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<IStrictModeHandler> _strictModeHandlerMock;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<IRuntimeInformationWrapper> _runtimeInformationWrapper;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapper;
    private ITwinHandler _target;
    private StrictModeSettings mockStrictModeSettingsValue = new StrictModeSettings();
    private Mock<IOptions<StrictModeSettings>> mockStrictModeSettings;
    private Mock<ISignatureHandler> _signatureHandlerMock;
    private CancellationToken cancellationToken = CancellationToken.None;


    [SetUp]
    public void Setup()
    {
        mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();
        mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
        mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);


        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _fileDownloadHandlerMock = new Mock<IFileDownloadHandler>();
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _twinActionsHandler = new Mock<ITwinActionsHandler>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _strictModeHandlerMock = new Mock<IStrictModeHandler>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _runtimeInformationWrapper = new Mock<IRuntimeInformationWrapper>();
        _fileStreamerWrapper = new Mock<IFileStreamerWrapper>();
        _signatureHandlerMock = new Mock<ISignatureHandler>();
        CreateTarget();
    }

    private void CreateTarget()
    {

        _target = new TwinHandler(_deviceClientMock.Object,
          _fileDownloadHandlerMock.Object,
          _fileUploaderHandlerMock.Object,
          _twinActionsHandler.Object,
          _loggerHandlerMock.Object,
          _runtimeInformationWrapper.Object,
          _strictModeHandlerMock.Object,
          _fileStreamerWrapper.Object,
          mockStrictModeSettings.Object,
          _signatureHandlerMock.Object);
    }

    [Test]
    public async Task GetTwinJsonAsync_ValidTwin_ReturnJson()
    {
        var twinProp = new TwinProperties();
        twinProp.Desired = new TwinCollection(MockHelper._baseDesierd);
        twinProp.Reported = new TwinCollection(MockHelper._baseReported);
        var twin = new Twin(twinProp);

        _deviceClientMock.Setup(x => x.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);

        var target = await _target.GetTwinJsonAsync();

        Assert.IsTrue(IsValidJson(target));
    }
    private bool IsValidJson(string str)
    {
        try
        {
            JToken.Parse(str);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoNewActions_NotUpdateReport()
    {
        var desired = new TwinDesired()
        {
            ChangeSpec = new TwinChangeSpec()
            {
                Id = "123",
                Patch = new TwinPatch()
                {
                    InstallSteps = new List<TwinAction>()
                    {   new TwinAction(),
                        new TwinAction()
                    }.ToArray()
                }
            }
        };

        var reported = new TwinReported()
        {
            ChangeSpec = new TwinReportedChangeSpec()
            {
                Id = "123",
                Patch = new TwinReportedPatch()
                {
                    InstallSteps = new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray()
                }
            }
        };

        CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()));

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _strictModeHandlerMock.Verify(dc => dc.CheckFileAccessPermissions(It.IsAny<TwinActionType>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_FirstTimeGetActions_ExecInprogressActions()
    {
        InitDataForTestInprogressActions();
        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, true);

        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NotInitial_ExecPendingActions()
    {
        InitDataForTestInprogressActions();
        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, false);

        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    private void InitDataForTestInprogressActions()
    {
        var desired = new TwinDesired()
        {
            ChangeSpec = new TwinChangeSpec()
            {
                Id = "123",
                Patch = new TwinPatch()
                {
                    InstallSteps = new List<TwinAction>()
                    {   new DownloadAction() {  DestinationPath = "123", Action = TwinActionType.SingularDownload},
                        new DownloadAction() { DestinationPath = "456", Action = TwinActionType.SingularDownload},
                        new DownloadAction() { DestinationPath = "789", Action = TwinActionType.SingularDownload},
                        new DownloadAction() { DestinationPath = "1", Action = TwinActionType.SingularDownload},
                        new DownloadAction() { DestinationPath = "12", Action = TwinActionType.SingularDownload},
                    }.ToArray()
                }
            }
        };

        var reported = new TwinReported()
        {
            ChangeSpec = new TwinReportedChangeSpec()
            {
                Id = "123",
                Patch = new TwinReportedPatch()
                {
                    InstallSteps = new List<TwinActionReported>()
                    {
                        new TwinActionReported() {Status = StatusType.Success},
                        new TwinActionReported() {Status = StatusType.Pending},
                        new TwinActionReported() {Status = StatusType.Pending},
                        new TwinActionReported() {Status = StatusType.SentForSignature},
                        new TwinActionReported() {Status = StatusType.InProgress}
                    }.ToArray()
                }
            }
        };

        CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

    }
    [Test]
    public async Task OnDesiredPropertiesUpdate_SuccessDownloadAction_NotExecuteDownload()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { Action = TwinActionType.SingularDownload},
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec()
        {
            Id = "123",
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray()
            }
        };

        CreateTwinMock(desired, reported);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NewDownloadAction_ExecuteDownload()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new DownloadAction() { Action = TwinActionType.SingularDownload, DestinationPath="abc"},
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NewDesiredId_ExecuteAllActions()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { Action=TwinActionType.SingularDownload},
                        new TwinAction() { Action=TwinActionType.SingularUpload}
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec()
        {
            Id = "456",
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray()
            }
        };

        CreateTwinMock(desired, reported);
        _twinActionsHandler.Setup(dc => dc.UpdateReportedChangeSpecAsync(It.IsAny<TwinReportedChangeSpec>(), It.IsAny<TwinPatchChangeSpec>(), It.IsAny<CancellationToken>()));
        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _strictModeHandlerMock.Verify(x => x.CheckFileAccessPermissions(It.IsAny<TwinActionType>(), It.IsAny<string>()), Times.Exactly(desired.Patch.InstallSteps.Count()));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_StrictModeWrongDestination_NotExecuteDownload()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new DownloadAction() { Action = TwinActionType.SingularDownload, DestinationPath=""},
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_StrictModeTrue_BashAndPowerShellActionsNotAllowed()
    {
        mockStrictModeSettingsValue.StrictMode = true;

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _twinActionsHandler.Verify(x => x.UpdateReportActionAsync(new List<ActionToReport>(), cancellationToken), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ValidateChangeSignFalse_UpdateReportedPropertiesCall()
    {
        var desired = new TwinChangeSpec();

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _signatureHandlerMock.Setup(sh => sh.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(false);

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ValidateChangeSignTrue_SignTwinKeyEventNotSend()
    {
        var desired = new TwinChangeSpec();

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _signatureHandlerMock.Setup(sh => sh.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(true);

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _signatureHandlerMock.Verify(sh => sh.SendSignTwinKeyEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ChangeSignNull_SignTwinKeyEventSend()
    {
        var desired = new TwinChangeSpec();

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported, changeSign: null);

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _signatureHandlerMock.Verify(sh => sh.SendSignTwinKeyEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoMethdUploadAction_MethodIsStream()
    {
        var desired = new TwinChangeSpec()
        {
            Patch = new TwinPatch()
            {
                TransitPackage = new List<TwinAction>() { new UploadAction() }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()));

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        Assert.AreEqual(FileUploadMethod.Stream, (desired.Patch.TransitPackage[0] as UploadAction).Method);
    }
    [Test]
    public async Task UpdateDeviceStateAsync_ValidState_Success()
    {
        var deviceState = DeviceStateType.Busy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        _target.UpdateDeviceStateAsync(deviceState, default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.DeviceState), deviceState.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceStateAsync_OnUpdateReportFailed_LogFailure()
    {
        var expectedErrorMessage = "my error";
        var deviceState = DeviceStateType.Busy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), deviceState.ToString(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception(expectedErrorMessage));
        _target.UpdateDeviceStateAsync(deviceState, default);
        _loggerHandlerMock.Verify(logger => logger.Error($"UpdateDeviceStateAsync failed: {expectedErrorMessage}"), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_OnExec_UpdateOSDescription()
    {
        var agentPlatformKey = nameof(TwinReported.AgentPlatform);
        var osDescription = "Mocked OS Description";
        _runtimeInformationWrapper.Setup(dc => dc.GetOSDescription()).Returns(osDescription);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(agentPlatformKey, osDescription, It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync(default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(agentPlatformKey, osDescription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_WindowsSupportedShells_UpdateWindowsSupportedShells()
    {
        var supportedShellsKey = nameof(TwinReported.SupportedShells);
        _runtimeInformationWrapper.Setup(dc => dc.IsOSPlatform(OSPlatform.Windows)).Returns(true);
        _fileStreamerWrapperMock.Setup(dc => dc.FileExists(It.IsAny<string>())).Returns(true);

        CreateTarget();
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey, It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync(default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey,
        new List<ShellType>() { ShellType.Cmd, ShellType.Powershell }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_LinuxSupportedShells_UpdateLinuxSupportedShells()
    {
        var supportedShellsKey = nameof(TwinReported.SupportedShells);
        _runtimeInformationWrapper.Setup(dc => dc.IsOSPlatform(OSPlatform.Linux)).Returns(true);
        _runtimeInformationWrapper.Setup(dc => dc.IsOSPlatform(OSPlatform.Windows)).Returns(false);
        _fileStreamerWrapperMock.Setup(dc => dc.FileExists(It.IsAny<string>())).Returns(true);

        CreateTarget();
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey, It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync(default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey,
        new List<ShellType>() { ShellType.Bash }, It.IsAny<CancellationToken>()), Times.Once);
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

        _target.UpdateDeviceCustomPropsAsync(null, cancellationToken);

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

        _target.UpdateDeviceCustomPropsAsync(newCustomProps, cancellationToken);


        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.Custom), It.Is<List<TwinReportedCustomProp>>(
            props => props.Count == 4), It.IsAny<CancellationToken>()), Times.Once);
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

        _target.UpdateDeviceCustomPropsAsync(newCustomProps, cancellationToken);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.Custom), It.Is<List<TwinReportedCustomProp>>(
            props => props.Count == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceStateAfterServiceRestartAsync_ValidState_Success()
    {
        var deviceState = DeviceStateType.Busy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        _target.UpdateDeviceStateAfterServiceRestartAsync(deviceState, default);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.DeviceStateAfterServiceRestart), deviceState.ToString(), It.IsAny<CancellationToken>()), Times.Once);
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

    private void CreateTwinMock(TwinChangeSpec twinChangeSpec, TwinReportedChangeSpec twinReportedChangeSpec, List<TwinReportedCustomProp>? twinReportedCustomProps = null, string? changeSign = "----")
    {
        var twin = MockHelper.CreateTwinMock(twinChangeSpec, twinReportedChangeSpec, null, null, twinReportedCustomProps, changeSign);
        _deviceClientMock.Setup(dc => dc.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);
        _signatureHandlerMock.Setup(dc => dc.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(true);

    }
}