using Moq;
using CloudPillar.Agent.Handlers;
using Shared.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Shared.Entities.Services;
using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Shared;
using Shared.Enums;
using Microsoft.Azure.Devices.Client.Transport;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class RunDiagnosticsHandlerTestFixture
{
    private IRunDiagnosticsHandler _target;

    private Mock<IFileUploaderHandler> _fileUploaderHandlerMock;
    private Mock<IOptions<RunDiagnosticsSettings>> _runDiagnosticsSettingsMock;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ICheckSumService> _checkSumServiceMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private Mock<FileStream> _fileStreamMock;
    private Mock<ILoggerHandler> _logger;
    private const int FILE_SIZE_BYTES = 128 * 1024;
    private const string ACTION_ID = "ActionId123";
    private string UPLOAD_FILE_PATH = Path.GetTempFileName();
    private string DOWNLOAD_FILE_PATH = Path.GetTempFileName();

    private CancellationToken cancellationToken = CancellationToken.None;


    [SetUp]
    public void Setup()
    {
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _checkSumServiceMock = new Mock<ICheckSumService>();
        _logger = new Mock<ILoggerHandler>();

        _runDiagnosticsSettingsMock = new Mock<IOptions<RunDiagnosticsSettings>>();
        _runDiagnosticsSettingsMock.Setup(x => x.Value).Returns(new RunDiagnosticsSettings() { FleSizBytes = FILE_SIZE_BYTES, PeriodicResponseWaitSeconds = 2, ResponseTimeoutMinutes = 0 });
        _fileStreamMock = new Mock<FileStream>(MockBehavior.Default, new object[] { "filePath", FileMode.Create });

        _fileStreamerWrapperMock.Setup(x => x.SetLength(It.IsAny<Stream>(), It.IsAny<long>()));
        CreateTarget();
    }

    [Test]
    public async Task CreateFileAsync_FullProcess_CreateFileWithContent()
    {
        await _target.HandleRunDiagnosticsProcess(cancellationToken);
        _fileStreamerWrapperMock.Verify(x => x.WriteAsync(It.IsAny<Stream>(), It.IsAny<byte[]>()), Times.Once);
    }

    [Test]
    public async Task UploadFileAsync_FullProcess_NotException()
    {
        _fileStreamerWrapperMock.Setup(x => x.GetTempFileName()).Returns(UPLOAD_FILE_PATH);
        // _fileUploaderHandlerMock.Setup(fh => fh.UploadFilesToBlobStorageAsync(uploadAction.FileName, uploadAction, actionToReport, cancellationToken, true));
        InitDataForTestInprogressActions();
        await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
        _deviceClientWrapperMock.Verify(dc => dc.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        // Assert.DoesNotThrowAsync(SendRequest);
    }

    private void InitDataForTestInprogressActions()
    {
        var changeSign = "changeSign";
        var desired = new TwinDesired()
        {
            ChangeSpecDiagnostics = new TwinChangeSpec()
            {
                Id = "123",
                Patch = new TwinPatch()
                {
                    TransitPackage = new List<TwinAction>()
                    {
                        new DownloadAction() { ActionId = ACTION_ID, Action = TwinActionType.SingularUpload, DestinationPath = DOWNLOAD_FILE_PATH}
                    }.ToArray()
                }
            }
        };

        var reported = new TwinReported()
        {
            ChangeSpecDiagnostics = new TwinReportedChangeSpec()
            {
                Id = "123",
                Patch = new TwinReportedPatch()
                {
                    TransitPackage = new List<TwinActionReported>()
                    {
                        new TwinActionReported() { Status = StatusType.Success}
                    }.ToArray()
                }
            }
        };
        var twin = MockHelper.CreateTwinMock(desired.ChangeSpecDiagnostics, reported.ChangeSpecDiagnostics, null, changeSign);
        _deviceClientWrapperMock.Setup(dc => dc.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);

    }
    // [Test]
    // public async Task UploadFileAsync_FullProcess_TrhowException()
    // {
    //     _fileUploaderHandlerMock.Setup(x => x.UploadFilesToBlobStorageAsync(It.IsAny<string>(), It.IsAny<UploadAction>(), It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
    //     .ThrowsAsync(new Exception());

    //     Assert.ThrowsAsync<Exception>(async () =>
    //     {
    //         await _target.UploadFileAsync(CancellationToken.None);
    //     });

    // }

    // [Test]
    // public async Task WaitingForResponseAsync_GetResponse_Success()
    // {
    //     var actionId = Guid.NewGuid().ToString();
    //     var checkSum = "testCheckSum";

    //     _runDiagnosticsSettingsMock.Setup(x => x.Value).Returns(new RunDiagnosticsSettings() { PeriodicResponseWaitSeconds = 1 });
    //     CreateTarget();

    //     _fileStreamerWrapperMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(_fileStreamMock.Object);
    //     _fileStreamerWrapperMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(_fileStreamMock.Object);
    //     _checkSumServiceMock.Setup(x => x.CalculateCheckSumAsync(It.IsAny<FileStream>(), It.IsAny<CheckSumType>())).ReturnsAsync(checkSum);

    //     CreateTwinMock(actionId, StatusType.Success);

    //     var result = await _target.WaitingForResponseAsync(actionId);

    //     Assert.AreEqual(StatusType.Success, result);
    // }

    // [Test]
    // public async Task WaitingForResponseAsync_Timeout_TrhowException()
    // {
    //     var actionId = Guid.NewGuid().ToString();
    //     _runDiagnosticsSettingsMock.Setup(x => x.Value).Returns(new RunDiagnosticsSettings() { PeriodicResponseWaitSeconds = 1, ResponseTimeoutMinutes = 0 });
    //     CreateTarget();
    //     CreateTwinMock(actionId, StatusType.Pending);

    //     Assert.ThrowsAsync<TimeoutException>(async () =>
    //        {
    //            await _target.WaitingForResponseAsync(actionId);
    //        });
    // }

    // private void CreateTwinMock(string actionId, StatusType statusType)
    // {
    //     var twinChangeSpec = new TwinChangeSpec()
    //     {
    //         Patch = new TwinPatch()
    //         {
    //             TransitPackage = new List<TwinAction>() { new TwinAction() { ActionId = actionId, Action = TwinActionType.SingularDownload } }.ToArray()
    //         }
    //     };
    //     var twinReportedChangeSpec = new TwinReportedChangeSpec()
    //     {
    //         Patch = new TwinReportedPatch()
    //         {
    //             TransitPackage = new List<TwinActionReported>() { new TwinActionReported() { Status = statusType } }.ToArray()
    //         }
    //     };
    //     var twin = MockHelper.CreateTwinMock(null, null, twinChangeSpec, twinReportedChangeSpec, new List<TwinReportedCustomProp>());
    //     _deviceClientWrapperMock.Setup(dc => dc.GetTwinAsync(CancellationToken.None)).ReturnsAsync(twin);
    // }

    private void CreateTarget()
    {
        _target = new RunDiagnosticsHandler(_fileUploaderHandlerMock.Object, _runDiagnosticsSettingsMock.Object, _fileStreamerWrapperMock.Object,
         _checkSumServiceMock.Object, _deviceClientWrapperMock.Object, _logger.Object);
    }

}
