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
    private Mock<ITwinReportHandler> _twinReportHandler;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<IStrictModeHandler> _strictModeHandlerMock;
    private ITwinHandler _target;
    private StrictModeSettings mockStrictModeSettingsValue = new StrictModeSettings();
    private Mock<IOptions<StrictModeSettings>> mockStrictModeSettings;
    private Mock<ISignatureHandler> _signatureHandlerMock;
    private Mock<IPeriodicUploaderHandler> _periodicUploaderHandlerMock;
    private CancellationToken cancellationToken = CancellationToken.None;
    private const string CHANGE_SPEC_ID = "123";


    [SetUp]
    public void Setup()
    {
        mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();
        mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
        mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);


        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _fileDownloadHandlerMock = new Mock<IFileDownloadHandler>();
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _twinReportHandler = new Mock<ITwinReportHandler>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _strictModeHandlerMock = new Mock<IStrictModeHandler>();
        _signatureHandlerMock = new Mock<ISignatureHandler>();
        _periodicUploaderHandlerMock = new Mock<IPeriodicUploaderHandler>();
        CreateTarget();
    }

    private void CreateTarget()
    {

        _target = new TwinHandler(_deviceClientMock.Object,
          _fileDownloadHandlerMock.Object,
          _fileUploaderHandlerMock.Object,
          _twinReportHandler.Object,
          _loggerHandlerMock.Object,
          _strictModeHandlerMock.Object,
          mockStrictModeSettings.Object,
          _signatureHandlerMock.Object,
          _periodicUploaderHandlerMock.Object);
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
                Id = CHANGE_SPEC_ID,
                Patch = new Dictionary<string, TwinAction[]>()
                {
                    { "InstallSteps", new List<TwinAction>()
                        {   new TwinAction(),
                        new TwinAction()
                        }.ToArray() }
                }
            }
        };

        var reported = new TwinReported()
        {
            ChangeSpec = new TwinReportedChangeSpec()
            {
                Id = CHANGE_SPEC_ID,
                Patch = new Dictionary<string, TwinActionReported[]>()
                    {
                        { "InstallSteps", new List<TwinActionReported>()
                            {  new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                            }.ToArray() }
                    }
            }
        };

        CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _strictModeHandlerMock.Verify(dc => dc.CheckFileAccessPermissions(It.IsAny<TwinActionType>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_FirstTimeGetActions_ExecInprogressActions()
    {
        InitDataForTestInprogressActions();
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, true);
        Task.Delay(1000).Wait();
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }
    
    [Test]
    public async Task OnDesiredPropertiesUpdate_FirstTime_InitCancellatioToken()
    {
        InitDataForTestInprogressActions();
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, true);
        Task.Delay(1000).Wait();
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.Is<CancellationToken>(x => x != null)), Times.Exactly(4));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NotInitial_ExecPendingActions()
    {
        InitDataForTestInprogressActions();
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, false);
        Task.Delay(1000).Wait();
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NewSpecId_InitDownloadFiles()
    {
        InitDataForTestInprogressActions($"{CHANGE_SPEC_ID}1");
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, false);

        _fileDownloadHandlerMock.Verify(dc => dc.InitDownloadsList(), Times.Once);
    }

    private void InitDataForTestInprogressActions(string reportId = CHANGE_SPEC_ID)
    {
        var desired = new TwinDesired()
        {
            ChangeSpec = new TwinChangeSpec()
            {
                Id = CHANGE_SPEC_ID,
                Patch = new Dictionary<string, TwinAction[]>()
                {
                    { "InstallSteps", new List<TwinAction>()
                        {   new DownloadAction() {  DestinationPath = "123", Action = TwinActionType.SingularDownload},
                            new DownloadAction() { DestinationPath = "456", Action = TwinActionType.SingularDownload},
                            new DownloadAction() { DestinationPath = "789", Action = TwinActionType.SingularDownload},
                            new DownloadAction() { DestinationPath = "1", Action = TwinActionType.SingularDownload},
                            new DownloadAction() { DestinationPath = "12", Action = TwinActionType.SingularDownload},
                        }.ToArray() }
                }
            }
        };

        var reported = new TwinReported()
        {
            ChangeSpec = new TwinReportedChangeSpec()
            {
                Id = reportId,
                Patch = new Dictionary<string, TwinActionReported[]>()
                    {
                        { "InstallSteps", new List<TwinActionReported>()
                            {  new TwinActionReported() {Status = StatusType.Success},
                                new TwinActionReported() {Status = StatusType.Pending},
                                new TwinActionReported() {Status = StatusType.Pending},
                                new TwinActionReported() {Status = StatusType.SentForSignature},
                                new TwinActionReported() {Status = StatusType.InProgress}
                            }.ToArray() }
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
            Id = CHANGE_SPEC_ID,
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "InstallSteps", new List<TwinAction>()
                    {   new DownloadAction() { Action = TwinActionType.SingularDownload},
                    }.ToArray() }
            }
        };

        var reported = new TwinReportedChangeSpec()
        {
            Id = CHANGE_SPEC_ID,
            Patch = new Dictionary<string, TwinActionReported[]>()
            {
                { "InstallSteps", new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray() }
            }

        };

        CreateTwinMock(desired, reported);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NewDownloadAction_ExecuteDownload()
    {
        var desired = new TwinChangeSpec()
        {
            Id = CHANGE_SPEC_ID,
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "InstallSteps", new List<TwinAction>()
                    {   new DownloadAction() { Action = TwinActionType.SingularDownload, DestinationPath="abc"},
                    }.ToArray() }
            }
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        Task.Delay(100).Wait();
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NewDesiredId_ExecuteAllActions()
    {
        var desired = new TwinChangeSpec()
        {
            Id = CHANGE_SPEC_ID,
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "InstallSteps", new List<TwinAction>()
                    {   new TwinAction() { Action=TwinActionType.SingularDownload},
                        new TwinAction() { Action=TwinActionType.SingularUpload}
                    }.ToArray() }
            }
        };

        var reported = new TwinReportedChangeSpec()
        {
            Id = "456",
            Patch = new Dictionary<string, TwinActionReported[]>()
            {
                { "InstallSteps", new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray() }
            }
        };

        CreateTwinMock(desired, reported);
        _twinReportHandler.Setup(dc => dc.UpdateReportedChangeSpecAsync(It.IsAny<TwinReportedChangeSpec>(), It.IsAny<TwinPatchChangeSpec>(), It.IsAny<CancellationToken>()));
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _strictModeHandlerMock.Verify(x => x.ReplaceRootById(It.IsAny<TwinActionType>(), It.IsAny<string>()), Times.Exactly(desired.Patch["InstallSteps"].Count()));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ReplaceRootByIdFailed_NotExecuteDownload()
    {
        var desired = new TwinChangeSpec()
        {
            Id = CHANGE_SPEC_ID,
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "TransitPackage", new List<TwinAction>()
                    {   new DownloadAction() { Action = TwinActionType.SingularDownload, DestinationPath=""},
                    }.ToArray() }
            }
        };

        var reported = new TwinReportedChangeSpec();
        _strictModeHandlerMock.Setup(x => x.ReplaceRootById(It.IsAny<TwinActionType>(), It.IsAny<string>())).Throws(new Exception());

        CreateTwinMock(desired, reported);
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_StrictModeTrue_BashAndPowerShellActionsNotAllowed()
    {
        mockStrictModeSettingsValue.StrictMode = true;

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _twinReportHandler.Verify(x => x.UpdateReportActionAsync(new List<ActionToReport>(), cancellationToken), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ValidateChangeSignFalse_UpdateReportedPropertiesCall()
    {
        var desired = new TwinChangeSpec();

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _signatureHandlerMock.Setup(sh => sh.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(false);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ValidateChangeSignTrue_SignTwinKeyEventNotSend()
    {
        var desired = new TwinChangeSpec();

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _signatureHandlerMock.Setup(sh => sh.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(true);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _signatureHandlerMock.Verify(sh => sh.SendSignTwinKeyEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoChangeSpecId_NoHandleActions()
    {
        var desired = new TwinChangeSpec()
        {
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "TransitPackage", new List<TwinAction>()
                    {   new UploadAction() {},
                    }.ToArray() }
            }
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileUploaderHandlerMock.Verify(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoChangeSpecId_UpdateReportedWithErrorMessage()
    {
        var desired = new TwinChangeSpec();

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.Is<string>(x => x == nameof(TwinReported.ChangeSpecId)), It.Is<string>(x => x == "There is no ID for changeSpec.."), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Test]
    public async Task OnDesiredPropertiesUpdate_ChangeSpecIdExists_UpdateReportedWithValueNull()
    {
        var desired = new TwinChangeSpec()
        {
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "TransitPackage", new TwinAction[0] }
            },
            Id = CHANGE_SPEC_ID
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.Is<string>(x => x == nameof(TwinReported.ChangeSpecId)), It.Is<string>(x => x == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ReportChangeSpecIsNull_ExecActions()
    {
        var desired = new TwinChangeSpec()
        {            
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "TransitPackage", new List<TwinAction>()
                    {   new UploadAction() 
                    }.ToArray() }
            },
            Id = CHANGE_SPEC_ID
        };

        var reported = null as TwinReportedChangeSpec;

        CreateTwinMock(desired, reported);

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        Task.Delay(1000).Wait();
        _fileUploaderHandlerMock.Verify(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ChangeSignNullStrictModeTrue_SignIsRequired()
    {
        mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock(true);
        mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);
        CreateTarget();

        var desired = new TwinChangeSpec() { Id = CHANGE_SPEC_ID };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported, changeSign: null);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.Is<string>(x => x == nameof(TwinReported.ChangeSign)), It.Is<string>(x => x == "Change sign is required"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ChangeSignNull_SignTwinKeyEventSend()
    {
        mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock(false);
        mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);
        CreateTarget();

        var desired = new TwinChangeSpec() { Id = CHANGE_SPEC_ID };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported, changeSign: null);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _signatureHandlerMock.Verify(sh => sh.SendSignTwinKeyEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoMethdUploadAction_MethodIsStream()
    {
        var desired = new TwinChangeSpec()
        {            
            Patch = new Dictionary<string, TwinAction[]>()
            {
                { "TransitPackage", new List<TwinAction>()
                    {   new UploadAction() 
                    }.ToArray() }
            }
        };

        var reported = new TwinReportedChangeSpec();

        CreateTwinMock(desired, reported);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        Assert.That((desired.Patch["TransitPackage"][0] as UploadAction)?.Method, Is.EqualTo(FileUploadMethod.Stream));
    }
    private void CreateTwinMock(TwinChangeSpec twinChangeSpec, TwinReportedChangeSpec twinReportedChangeSpec, string? changeSign = "----")
    {
        var twin = MockHelper.CreateTwinMock(twinChangeSpec, twinReportedChangeSpec, null, null, null, changeSign);
        _deviceClientMock.Setup(dc => dc.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);
        _signatureHandlerMock.Setup(dc => dc.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(true);

    }
}