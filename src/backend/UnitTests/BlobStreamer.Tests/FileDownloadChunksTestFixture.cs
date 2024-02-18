using Backend.BlobStreamer.Services;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Castle.Core.Logging;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Entities.Messages;
using Shared.Logger;

namespace Backend.BlobStreamer.Tests;

public class FileDownloadChunksTestFixture
{
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<IBlobService> _blobServiceMock;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private FileDownloadChunksService _target;
    private Uri _blobStreamerUrl;
    private const string _deviceId = "testDevice";
    private const int _actionIndexd = 123;
    private const string _fileName = "testFile.bin";
    private const string _changeSpecId = "1.2.3";
    private const string _rangesCount = "2";
    private const int _chunkSize = 1024;
    private const long _startPosition = 0L;
    private const long _rangeSize = 1024L;
    private const long _blobSize = 2048L;

    [SetUp]
    public void Setup()
    {
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _blobServiceMock = new Mock<IBlobService>();
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _target = new FileDownloadChunksService(_loggerHandlerMock.Object, _mockEnvironmentsWrapper.Object, _blobServiceMock.Object);
    }

    [Test]
    public async Task SendFileDownloadAsync_ThrowsArgumentNullException_WhenChangeSpecIdIsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => { await _target.SendFileDownloadAsync("deviceId", new FileDownloadEvent()); });
    }

    [Test]
    public async Task SendFileDownloadAsync_SendsRangeRequests()
    {
        // _blobServiceMock.Setup(b => b.GetBlobMetadataAsync(_fileName)).Returns(new BlobData() { Length = _blobSize });
        await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd,
            ChangeSpecId = _changeSpecId
        });

        for (long offset = _startPosition, rangeIndex = 0; offset < _blobSize; offset += _rangeSize, rangeIndex++)
        {
            _blobServiceMock.Verify(service => service.SendRangeByChunksAsync(_deviceId, _changeSpecId, _fileName, _chunkSize, (int)_rangeSize, (int)rangeIndex, _startPosition, _actionIndexd, (int)2), Times.Once);
        }
    }
}



public struct BlobData
{
    public long Length { get; set; }
}