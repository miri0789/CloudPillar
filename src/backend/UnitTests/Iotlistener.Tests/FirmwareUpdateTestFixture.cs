using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Services;
using Moq;
using Shared.Entities.Messages;

namespace Backend.Iotlistener.Tests;
public class FileDownloadTestFixture
{
    private FileDownloadService _target;
    private Mock<ISendQueueMessagesService> _mockSendQueueMessagesService;

    private const string _deviceId = "testDevice";
    private const int _actionIndexd = 123;
    private const string _fileName = "testFile.bin";
    private const string _changeSpecId = "1.2.3";
    private const int _chunkSize = 1024;
    private const long _startPosition = 0L;


    [SetUp]
    public void Setup()
    {
        _mockSendQueueMessagesService = new Mock<ISendQueueMessagesService>();
        _target = new FileDownloadService(_mockSendQueueMessagesService.Object);
    }

    [Test]
    public async Task SendFileDownloadAsync_WhenCalled_ShouldSendFileDownloadMessage()
    {
        var fileDownloadEvent = new FileDownloadEvent
        {
            ActionIndex = _actionIndexd,
            FileName = _fileName,
            ChangeSpecId = _changeSpecId,
            ChunkSize = _chunkSize,
            StartPosition = _startPosition
        };
        await _target.SendFileDownloadAsync(_deviceId, fileDownloadEvent);
        _mockSendQueueMessagesService.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<FileDownloadEvent>()), Times.Once);
    }
}