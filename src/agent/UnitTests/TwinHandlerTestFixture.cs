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
using Shared.Entities.Utilities;

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
    private const string PATCH_KEY = "transitPackage";
    private string changeSignKey;
    private string changeSpecIdKey;


    [SetUp]
    public void Setup()
    {
        mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();
        mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
        mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);

        changeSignKey = TwinConstants.CHANGE_SPEC_NAME.GetSignKeyByChangeSpec();
        changeSpecIdKey = TwinConstants.CHANGE_SPEC_NAME.GetChangeSpecIdKeyByChangeSpecKey();

        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _fileDownloadHandlerMock = new Mock<IFileDownloadHandler>();
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _twinReportHandler = new Mock<ITwinReportHandler>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _strictModeHandlerMock = new Mock<IStrictModeHandler>();
        _signatureHandlerMock = new Mock<ISignatureHandler>();
        _periodicUploaderHandlerMock = new Mock<IPeriodicUploaderHandler>();
        _fileDownloadHandlerMock.Setup(dc => dc.AddFileDownload(It.IsAny<ActionToReport>())).Returns(true);
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

    private Dictionary<string, TwinChangeSpec> GetDefaultDesiredChangeSpec(Dictionary<string, TwinAction[]> patch = null, string id = "")
    {
        var desiredChangeSpec = new Dictionary<string, TwinChangeSpec>()
            {
                {
                TwinConstants.CHANGE_SPEC_NAME, new TwinChangeSpec() {
                        Patch = patch ?? new Dictionary<string, TwinAction[]>()
                        {
                            { PATCH_KEY, new TwinAction[0] }
                        },
                        Id = string.IsNullOrWhiteSpace(id)?  CHANGE_SPEC_ID : id
                    }
                }
            };

        return desiredChangeSpec;
    }
    private Dictionary<string, TwinReportedChangeSpec> GetDefaultReportedChangeSpec(Dictionary<string, TwinActionReported[]> patch = null, string id = "")
    {

        var reportedChangeSpec = new Dictionary<string, TwinReportedChangeSpec>
            {
                {
                    TwinConstants.CHANGE_SPEC_NAME, new TwinReportedChangeSpec()
                    {
                        Id = string.IsNullOrWhiteSpace(id)?  CHANGE_SPEC_ID : id,
                        Patch = patch ?? new Dictionary<string, TwinActionReported[]>()
                        {
                            { PATCH_KEY, new TwinActionReported[0] }
                        }
                    }
                }
            };
        return reportedChangeSpec;
    }
    private Dictionary<string, string> GetDefaultChangeSign()
    {
        var changeSign = new Dictionary<string, string>()
            {
                { changeSignKey, "changeSign" }
            };
        return changeSign;
    }

    [Test]
    public async Task GetTwinJsonAsync_ValidTwin_ReturnJson()
    {
        var twinProp = new TwinProperties()
        {
            Desired = new TwinCollection(MockHelper._baseDesierd),
            Reported = new TwinCollection(MockHelper._baseReported)
        };
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

        var reportedPatch = new Dictionary<string, TwinActionReported[]>(){
            {
                PATCH_KEY, new TwinActionReported[]{
                        new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                }
            }
        };

        var desired = GetDefaultDesiredChangeSpec();//InstallSteps
        var reported = GetDefaultReportedChangeSpec(reportedPatch);


        CreateTwinMock(desired, reported, GetDefaultChangeSign());
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _strictModeHandlerMock.Verify(dc => dc.CheckFileAccessPermissions(It.IsAny<TwinActionType>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_FirstTimeGetActions_ExecInprogressActions()
    {
        InitDataForTestInprogressActions();
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, true);
        Task.Delay(10).Wait();
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_FirstTime_InitCancellatioToken()
    {
        InitDataForTestInprogressActions();
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, true);
        Task.Delay(10).Wait();
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.Is<CancellationToken>(x => x != null)), Times.Exactly(4));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NotInitial_ExecPendingActions()
    {
        InitDataForTestInprogressActions();
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None, false);
        Task.Delay(10).Wait();
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
            ChangeSpec = new Dictionary<string, TwinChangeSpec>()
        };

        desired.ChangeSpec.Add(TwinConstants.CHANGE_SPEC_NAME, new TwinChangeSpec()
        {
            Id = CHANGE_SPEC_ID,
            Patch = new Dictionary<string, TwinAction[]>()
             {
                {
                    "InstallSteps", new List<TwinAction>()
                    {
                        new DownloadAction() { DestinationPath = "123", Action = TwinActionType.SingularDownload },
                        new DownloadAction() { DestinationPath = "456", Action = TwinActionType.SingularDownload },
                        new DownloadAction() { DestinationPath = "789", Action = TwinActionType.SingularDownload },
                        new DownloadAction() { DestinationPath = "1", Action = TwinActionType.SingularDownload },
                        new DownloadAction() { DestinationPath = "12", Action = TwinActionType.SingularDownload },
                    }.ToArray()
                }
             }
        });

        var reported = new TwinReported()
        {
            ChangeSpec = new Dictionary<string, TwinReportedChangeSpec>()
        };

        reported.ChangeSpec.Add(TwinConstants.CHANGE_SPEC_NAME, new TwinReportedChangeSpec()
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
        });


        CreateTwinMock(desired.ChangeSpec, reported.ChangeSpec, GetDefaultChangeSign());
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

    }
    [Test]
    public async Task OnDesiredPropertiesUpdate_SuccessDownloadAction_NotExecuteDownload()
    {
        var desired = GetDefaultDesiredChangeSpec();
        desired.First().Value.Patch[PATCH_KEY] = new TwinAction[]{
                        new DownloadAction() { Action = TwinActionType.SingularDownload},
        };

        var reported = GetDefaultReportedChangeSpec();
        reported.First().Value.Patch[PATCH_KEY] = new TwinActionReported[]{
                       new TwinActionReported() {Status = StatusType.Success},
        };

        CreateTwinMock(desired, reported, GetDefaultChangeSign());
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NewDownloadAction_ExecuteDownload()
    {
        var desired = GetDefaultDesiredChangeSpec();
        desired.First().Value.Patch[PATCH_KEY] = new TwinAction[]{
                        new DownloadAction() { Action = TwinActionType.SingularDownload, DestinationPath="abc"}
        };
        //  new TwinChangeSpec()
        // { 
        //     Id = CHANGE_SPEC_ID,
        //     Patch = new Dictionary<string, TwinAction[]>()
        //     {
        //         { "InstallSteps", new List<TwinAction>()
        //             {   new DownloadAction() { Action = TwinActionType.SingularDownload, DestinationPath="abc"},
        //             }.ToArray() }
        //     }
        // };

        var reported = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desired, reported, GetDefaultChangeSign());
        _fileDownloadHandlerMock.Setup(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        Task.Delay(10).Wait();
        _fileDownloadHandlerMock.Verify(dc => dc.InitFileDownloadAsync(It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NewDesiredId_ExecuteAllActions()
    {


        var desiredPatch = new Dictionary<string, TwinAction[]>(){
            {
                PATCH_KEY, new TwinAction[]{
                       new TwinAction() { Action=TwinActionType.SingularDownload},
                        new TwinAction() { Action=TwinActionType.SingularUpload}
                }
            }
        };
        var desired = GetDefaultDesiredChangeSpec(desiredPatch);

        var reportedPatch = new Dictionary<string, TwinActionReported[]>(){
            {
                PATCH_KEY, new TwinActionReported[]{
                       new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                }
            }
        };
        var reported = GetDefaultReportedChangeSpec(reportedPatch, "456");

        CreateTwinMock(desired, reported, GetDefaultChangeSign());
        _twinReportHandler.Setup(dc => dc.UpdateReportedChangeSpecAsync(It.IsAny<TwinReportedChangeSpec>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _strictModeHandlerMock.Verify(x => x.ReplaceRootById(It.IsAny<TwinActionType>(), It.IsAny<string>()), Times.Exactly(desired.First().Value.Patch[PATCH_KEY].Count()));
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ReplaceRootByIdFailed_NotExecuteDownload()
    {


        var desiredPatch = new Dictionary<string, TwinAction[]>(){
            {
                PATCH_KEY, new TwinAction[]{
                       new DownloadAction() { Action = TwinActionType.SingularDownload, DestinationPath=""}
                }
            }
        };
        var desired = GetDefaultDesiredChangeSpec(desiredPatch);

        var reported = new Dictionary<string, TwinReportedChangeSpec>();

        _strictModeHandlerMock.Setup(x => x.ReplaceRootById(It.IsAny<TwinActionType>(), It.IsAny<string>())).Throws(new Exception());

        CreateTwinMock(desired, reported, GetDefaultChangeSign());
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
        var desiredChangeSpec = GetDefaultDesiredChangeSpec();
        var reportedChangeSpec = GetDefaultReportedChangeSpec();

        CreateTwinMock(desiredChangeSpec, reportedChangeSpec, GetDefaultChangeSign());
        _signatureHandlerMock.Setup(sh => sh.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(false);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.Is<string>(x => x == changeSignKey), It.Is<object>(x => x.ToString() == $"Twin Change signature for {changeSignKey} is invalid"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ValidateChangeSignTrue_SignTwinKeyEventNotSend()
    {
        var desiredChangeSpec = new Dictionary<string, TwinChangeSpec>();
        var reportedChangeSpec = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desiredChangeSpec, reportedChangeSpec, GetDefaultChangeSign());

        _signatureHandlerMock.Setup(sh => sh.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(true);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _signatureHandlerMock.Verify(sh => sh.SendSignTwinKeyEventAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoChangeSpecId_NoHandleActions()
    {

        var desired = new Dictionary<string, TwinChangeSpec>()
            {
                {
                TwinConstants.CHANGE_SPEC_NAME, new TwinChangeSpec() {
                        Patch = new Dictionary<string, TwinAction[]>()
                        {
                            { PATCH_KEY, new TwinAction[0] }
                        }
                    }
                }
            };


        var reportedChangeSpec = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desired, reportedChangeSpec, GetDefaultChangeSign());

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _fileUploaderHandlerMock.Verify(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoChangeSpecId_UpdateReportedWithErrorMessage()
    {
        var desiredChangeSpec = GetDefaultDesiredChangeSpec();
        desiredChangeSpec.First().Value.Id = null;

        var reportedChangeSpec = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desiredChangeSpec, reportedChangeSpec, GetDefaultChangeSign());

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.Is<string>(x => x == changeSpecIdKey), It.Is<string>(x => x == "There is no ID for changeSpec.."), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Test]
    public async Task OnDesiredPropertiesUpdate_ChangeSpecIdExists_UpdateReportedWithValueNull()
    {
        var desired = GetDefaultDesiredChangeSpec();
        var reportedChangeSpec = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desired, reportedChangeSpec, GetDefaultChangeSign());

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.Is<string>(x => x == changeSpecIdKey), It.Is<string>(x => x == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ReportChangeSpecIsNull_ExecActions()
    {

        var desiredPatch = new Dictionary<string, TwinAction[]>(){
            {
                PATCH_KEY, new TwinAction[]{
                        new UploadAction() {},
                }
            }
        };
        var desired = GetDefaultDesiredChangeSpec(desiredPatch);

        var reported = null as Dictionary<string, TwinReportedChangeSpec>;

        CreateTwinMock(desired, reported, GetDefaultChangeSign());

        _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        Task.Delay(10).Wait();
        _fileUploaderHandlerMock.Verify(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ChangeSignNullStrictModeTrue_SignIsRequired()
    {
        mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock(true);
        mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);
        CreateTarget();

        var desired = GetDefaultDesiredChangeSpec();
        var reported = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desired, reported, null);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _deviceClientMock.Verify(x => x.UpdateReportedPropertiesAsync(It.Is<string>(x => x == changeSignKey), It.Is<string>(x => x == "Change sign is required"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_ChangeSignNull_SignTwinKeyEventSend()
    {

        mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock(false);
        mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);
        CreateTarget();

        var desired = GetDefaultDesiredChangeSpec();
        var reported = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desired, reported, null);

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        _signatureHandlerMock.Verify(sh => sh.SendSignTwinKeyEventAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDesiredPropertiesUpdate_NoMethodUploadAction_MethodIsStream()
    {
        var desiredPatch = new Dictionary<string, TwinAction[]>(){
            {
                PATCH_KEY, new TwinAction[]{ new UploadAction() }
            }
        };
        var desired = GetDefaultDesiredChangeSpec(desiredPatch);

        var reported = new Dictionary<string, TwinReportedChangeSpec>();

        CreateTwinMock(desired, reported, GetDefaultChangeSign());
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()));

        await _target.OnDesiredPropertiesUpdateAsync(CancellationToken.None);
        Assert.That((desired.First().Value.Patch[PATCH_KEY][0] as UploadAction)?.Method, Is.EqualTo(FileUploadMethod.Stream));
    }
    private void CreateTwinMock(Dictionary<string, TwinChangeSpec> twinChangeSpec,
    Dictionary<string, TwinReportedChangeSpec> twinReportedChangeSpec, Dictionary<string, string>? changeSign, List<TwinReportedCustomProp>? twinReportedCustomProps = null)
    {
        var twin = MockHelper.CreateTwinMock(twinChangeSpec, twinReportedChangeSpec, twinReportedCustomProps, changeSign);
        _twinReportHandler.Setup(dc => dc.SetTwinReported(cancellationToken)).ReturnsAsync(twin);
        _signatureHandlerMock.Setup(dc => dc.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(true);
    }
}