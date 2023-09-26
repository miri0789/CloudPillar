using System.Runtime.InteropServices;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Shared;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class TwinHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<IFileDownloadHandler> _fileDownloadHandlerMock;
    private Mock<IFileUploaderHandler> _fileUploaderHandlerMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<IRuntimeInformationWrapper> _runtimeInformationWrapper;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapper;
    private ITwinHandler _target;

    private string _baseDesierd = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";
    private string _baseReported = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";

    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _fileDownloadHandlerMock = new Mock<IFileDownloadHandler>();
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _runtimeInformationWrapper = new Mock<IRuntimeInformationWrapper>();
        _fileStreamerWrapper = new Mock<IFileStreamerWrapper>();
        CreateTarget();
    }

    private void CreateTarget()
    {

        _target = new TwinHandler(_deviceClientMock.Object,
          _fileDownloadHandlerMock.Object,
          _fileUploaderHandlerMock.Object,
          _loggerHandlerMock.Object,
          _runtimeInformationWrapper.Object,
          _fileStreamerWrapper.Object);
    }

    [Test]
    public async Task GetTwinJsonAsync_ValidTwin_ReturnJson()
    {
        var twinProp = new TwinProperties();
        twinProp.Desired = new TwinCollection(_baseDesierd);
        twinProp.Reported = new TwinCollection(_baseReported);
        var twin = new Twin(twinProp);

        _deviceClientMock.Setup(x => x.GetTwinAsync()).ReturnsAsync(twin);

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
    public async Task HandleTwinActionsAsync_NoNewActions_NotUpdateReport()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { ActionId = "123"},
                        new TwinAction() { ActionId = "456"}
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec()
        {
            Id = "123",
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray()
            }
        };

        CreateTwinMock(desired, reported);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>()));

        _target.HandleTwinActionsAsync(CancellationToken.None);
        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Test]
    public async Task HandleTwinActionsAsync_SuccessDownloadAction_NotExecuteDownload()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { ActionId = "123", Action = TwinActionType.SingularDownload},
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
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<DownloadAction>(), It.IsAny<ActionToReport>()));

        _target.HandleTwinActionsAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<DownloadAction>(), It.IsAny<ActionToReport>()), Times.Never);
    }

    [Test]
    public async Task HandleTwinActionsAsync_NewDownloadAction_ExecuteDownload()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { ActionId = "123", Action = TwinActionType.SingularDownload},
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<DownloadAction>(), It.IsAny<ActionToReport>()));

        _target.HandleTwinActionsAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<DownloadAction>(), It.IsAny<ActionToReport>()), Times.Once);
    }

    [Test]
    public async Task HandleTwinActionsAsync_NewDesiredId_ExecuteAllActions()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { ActionId = "123",Action=TwinActionType.SingularDownload,},
                        new TwinAction() { ActionId = "456",Action=TwinActionType.SingularUpload}
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
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>()));

        _target.HandleTwinActionsAsync(CancellationToken.None);
        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(3));
    }


    [Test]
    public async Task UpdateDeviceStateAsync_ValidState_Success()
    {
        var deviceState = DeviceStateType.Buzy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), deviceState))
                       .Returns(Task.CompletedTask);

        _target.UpdateDeviceStateAsync(deviceState);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(nameof(TwinReported.DeviceState), deviceState), Times.Once);
    }

    [Test]
    public async Task UpdateDeviceStateAsync_OnUpdateReportFailed_LogFailure()
    {
        var expectedErrorMessage = "my error";
        var deviceState = DeviceStateType.Buzy;
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), deviceState))
                       .ThrowsAsync(new Exception(expectedErrorMessage));
        _target.UpdateDeviceStateAsync(deviceState);
        _loggerHandlerMock.Verify(logger => logger.Error($"UpdateDeviceStateAsync failed: {expectedErrorMessage}"), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_OnExec_UpdateOSDescription()
    {
        var agentPlatformKey = nameof(TwinReported.AgentPlatform);
        var osDescription = "Mocked OS Description";
        _runtimeInformationWrapper.Setup(dc => dc.GetOSDescription()).Returns(osDescription);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(agentPlatformKey, osDescription))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync();

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(agentPlatformKey, osDescription), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_WindowsSupportedShells_UpdateWindowsSupportedShells()
    {
        var supportedShellsKey = nameof(TwinReported.SupportedShells);
        _runtimeInformationWrapper.Setup(dc => dc.IsOSPlatform(OSPlatform.Windows)).Returns(true);
        _fileStreamerWrapper.Setup(dc => dc.Exists(It.IsAny<string>())).Returns(true);

        CreateTarget();
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey, It.IsAny<object>()))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync();

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey,
        new List<ShellType>() { ShellType.Cmd, ShellType.Powershell, ShellType.Bash }), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_LinuxSupportedShells_UpdateLinuxSupportedShells()
    {
        var supportedShellsKey = nameof(TwinReported.SupportedShells);
        _runtimeInformationWrapper.Setup(dc => dc.IsOSPlatform(OSPlatform.Linux)).Returns(true);
        _runtimeInformationWrapper.Setup(dc => dc.IsOSPlatform(OSPlatform.Windows)).Returns(false);
        _fileStreamerWrapper.Setup(dc => dc.Exists(It.IsAny<string>())).Returns(true);

        CreateTarget();
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey, It.IsAny<object>()))
                       .Returns(Task.CompletedTask);

        await _target.InitReportDeviceParamsAsync();

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(supportedShellsKey,
        new List<ShellType>() { ShellType.Bash, ShellType.Powershell }), Times.Once);
    }

    [Test]
    public async Task InitReportDeviceParamsAsync_OnUpdateReportFailed_LogFailure()
    {
        var expectedErrorMessage = "Simulated error message";
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>()))
                       .ThrowsAsync(new Exception(expectedErrorMessage));

        await _target.InitReportDeviceParamsAsync();
        _loggerHandlerMock.Verify(logger => logger.Error($"InitReportedDeviceParams failed: {expectedErrorMessage}"), Times.Once);
    }


    [Test]
    public async Task UpdateReportActionAsync_ValidReport_CallToUpdateReport()
    {
        var actionsToReported = CreateReportForUpdating();

        await _target.UpdateReportActionAsync(actionsToReported);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(
            nameof(TwinReported.ChangeSpec), It.IsAny<string>()), Times.Once);
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

    private void CreateTwinMock(TwinChangeSpec desired, TwinReportedChangeSpec reported)
    {
        var desiredJson = JObject.Parse(_baseDesierd);
        desiredJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinDesired()
        {
            ChangeSpec = desired
        })));
        var reportedJson = JObject.Parse(_baseReported);
        reportedJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinReported()
        {
            ChangeSpec = reported
        })));
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        };
        var twinProp = new TwinProperties()
        {
            Desired = new TwinCollection(JsonConvert.SerializeObject(desiredJson, settings)),
            Reported = new TwinCollection(JsonConvert.SerializeObject(reportedJson, settings))
        };
        var twin = new Twin(twinProp);
        _deviceClientMock.Setup(dc => dc.GetTwinAsync()).ReturnsAsync(twin);
    }


}