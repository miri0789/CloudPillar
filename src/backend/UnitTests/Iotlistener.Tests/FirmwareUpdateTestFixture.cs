using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Moq;
using Shared.Entities.Messages;
using Shared.Logger;
using Backend.Iotlistener.Models;

namespace Backend.Iotlistener.Tests;
public class FileDownloadTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private FileDownloadService _target;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLoggerHandler;
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
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _blobStreamerUrl = new Uri("http://example.com/");
        _mockEnvironmentsWrapper.Setup(f => f.blobStreamerUrl).Returns(_blobStreamerUrl.AbsoluteUri);
        _mockEnvironmentsWrapper.Setup(f => f.rangeCalculateType).Returns(Models.Enums.RangeCalculateType.Bytes);
        _mockEnvironmentsWrapper.Setup(f => f.rangeBytes).Returns(_rangeSize);
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/Metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobData() { Length = _blobSize });
        _target = new FileDownloadService(_httpRequestorServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);

        _httpRequestorServiceMock.Setup(service =>
                        service.SendRequest<bool>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }



    [Test]
    public async Task SendFileDownloadAsync_SendsRangeRequests()
    {
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
            string blobRangeUrl = BuildBlobRangeUrl(rangeIndex, offset);
            _httpRequestorServiceMock.Verify(service =>
                service.SendRequest<bool>(blobRangeUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task SendFileDownloadAsync__MsgWithEndPosition_SendsOneRange()
    {
        await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd,
            EndPosition = 123,
            ChangeSpecId = _changeSpecId
        });

        _httpRequestorServiceMock.Verify(service =>
                        service.SendRequest(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                        Times.Once);

    }


    [Test]
    public async Task SendFileDownloadAsync_MessageWithCompleteRanges_NotSendExistRanges()
    {

        await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd,
            CompletedRanges = "0",
            ChangeSpecId = _changeSpecId
        });

        string blobRangeUrl = BuildBlobRangeUrl(0, 0);
        _httpRequestorServiceMock.Verify(service =>
            service.SendRequest<bool>(blobRangeUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);

    }


    [Test]
    public async Task SendFileDownloadAsync_SendBlobFalseResponse_BreakSendsRanges()
    {
        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/Metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobData() { Length = _blobSize * 100 });

        _httpRequestorServiceMock.Setup(service =>
                        service.SendRequest<bool>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd,
            ChangeSpecId = _changeSpecId
        });

        _httpRequestorServiceMock.Verify(service =>
                        service.SendRequest<bool>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                        Times.Exactly(4));

    }

    private string BuildBlobRangeUrl(long rangeIndex = 0, long startPosition = 0)
    {
        return $"{_blobStreamerUrl.AbsoluteUri}blob/Range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex={rangeIndex}&startPosition={startPosition}&actionIndex={_actionIndexd}&rangesCount={_rangesCount}&changeSpecId={_changeSpecId}";
    }



    [Test]
    public async Task SendFileDownloadAsync_GetBlobSizeFails_SendErrMsg()
    {
        var errMsg = "Failed to retrieve blob size.";
        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/Metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errMsg));

        await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd,
            ChangeSpecId = _changeSpecId
        });
        var errUrl = $"{_blobStreamerUrl.AbsoluteUri}blob/RangeError?deviceId={_deviceId}&fileName={_fileName}&actionIndex={_actionIndexd}&error={errMsg}&changeSpecId={_changeSpecId}";
        _httpRequestorServiceMock.Verify(service =>
               service.SendRequest(errUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
               Times.Once);
    }


}
