using NUnit.Framework;
using Moq;
using System.IO;
using System.Threading.Tasks;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

[TestFixture]
public class FileDownloadHandlerTestFixture
{
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private IFileDownloadHandler _fileDownloadHandler;

    private DownloadAction _downloadAction = new DownloadAction()
    {
        ActionId = "action123",
        Source = "file.txt",
        DestinationPath = "C:\\Downloads"
    };
    private ActionToReport _actionToReport = new ActionToReport()
    {
        ReportIndex = 1,
        TwinReport = new TwinActionReported()
    };

    [SetUp]
    public void Setup()
    {
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _fileDownloadHandler = new FileDownloadHandler(_fileStreamerWrapperMock.Object, _d2CMessengerHandlerMock.Object);
    }


    [Test]
    public async Task InitFileDownloadAsync_Add_SendFirmwareUpdateEvent()
    {
        _d2CMessengerHandlerMock.Setup(dc => dc.SendFirmwareUpdateEventAsync(_downloadAction.Source, _downloadAction.ActionId, null, null));

        await _fileDownloadHandler.InitFileDownloadAsync(_downloadAction, _actionToReport);

        _d2CMessengerHandlerMock.Verify(mf => mf.SendFirmwareUpdateEventAsync(_downloadAction.Source, _downloadAction.ActionId, null, null), Times.Once);

    }

    [Test]
    public async Task InitFileDownloadAsync_Failure_ThrowException()
    {
        _d2CMessengerHandlerMock.Setup(dc =>
                dc.SendFirmwareUpdateEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<long?>()))
                .ThrowsAsync(new Exception());

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _fileDownloadHandler.InitFileDownloadAsync(_downloadAction, _actionToReport);
        });

    }


    [Test]
    public async Task HandleDownloadMessageAsync_HandleMessage_ReturnReport()
    {
        await _fileDownloadHandler.InitFileDownloadAsync(_downloadAction, _actionToReport);

        var message = new DownloadBlobChunkMessage
        {
            ActionId = _downloadAction.ActionId,
            FileName = _downloadAction.Source,
            Offset = 0,
            Data = new byte[1024],
            FileSize = 2048
        };
        _fileStreamerWrapperMock.Setup(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()));

        var report = await _fileDownloadHandler.HandleDownloadMessageAsync(message);
        Assert.AreEqual(report.TwinReport.Status, StatusType.InProgress);
        Assert.AreEqual(report.TwinReport.Progress, 50);
        report = await _fileDownloadHandler.HandleDownloadMessageAsync(message);
        Assert.AreEqual(report.TwinReport.Status, StatusType.Success);
        _fileStreamerWrapperMock.Verify(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()), Times.Exactly(2));
    }

    [Test]
    public async Task HandleDownloadMessageAsync_NotExistFile_ThrowException()
    {
        var message = new DownloadBlobChunkMessage
        {
            ActionId = "NotExistActionId",
            FileName = _downloadAction.Source,
            Offset = 0,
            Data = new byte[1024],
            FileSize = 2048
        };
        _fileStreamerWrapperMock.Setup(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()));

        Assert.ThrowsAsync<ArgumentException>(async () =>
               {
                   await _fileDownloadHandler.HandleDownloadMessageAsync(message);
               });
        _fileStreamerWrapperMock.Verify(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()), Times.Never);
    }

}
