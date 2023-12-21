using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Moq;
using Shared.Entities.Messages;
using Shared.Logger;
using Backend.Iotlistener.Models;

namespace Backend.Iotlistener.Tests;
public class FirmwareUpdateTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private FirmwareUpdateService _target;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private Uri _blobStreamerUrl;

    private const string _deviceId = "testDevice";
    private const int _actionIndexd = 123;
    private const string _fileName = "testFile.bin";
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
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobData() { Length = _blobSize });
        _target = new FirmwareUpdateService(_httpRequestorServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);
        
        _httpRequestorServiceMock.Setup(service =>
                        service.SendRequest<bool>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }



    [Test]
    public async Task SendFirmwareUpdateAsync_SendsRangeRequests()
    {

        _httpRequestorServiceMock
                    .Setup(service => service.SendRequest(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()));


        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd
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
    public async Task SendFirmwareUpdateAsync__MsgWithEndPosition_SendsOneRange()
    {

        _httpRequestorServiceMock
                    .Setup(service => service.SendRequest(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()));

        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd,
            EndPosition = 123
        });

        _httpRequestorServiceMock.Verify(service =>
                        service.SendRequest(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                        Times.Once);

    }


    [Test]
    public async Task SendFirmwareUpdateAsync_SendBlobFalseResponse_BreakSendsRanges()
    {

        _httpRequestorServiceMock
                    .Setup(service => service.SendRequest(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()));

        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobData() { Length = _blobSize * 100 });

        _httpRequestorServiceMock.Setup(service =>
                        service.SendRequest<bool>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd
        });

        
        

        _httpRequestorServiceMock.Verify(service =>
                        service.SendRequest<bool>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                        Times.Exactly(4));

    }


    private string BuildBlobRangeUrl(long rangeIndex = 0, long startPosition = 0)
    {
        return $"{_blobStreamerUrl.AbsoluteUri}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex={rangeIndex}&startPosition={startPosition}&actionIndex={_actionIndexd}&rangesCount={_rangesCount}";
    }



    [Test]
    public async Task SendFirmwareUpdateAsync_GetBlobSizeFails_SendErrMsg()
    {
        var errMsg = "Failed to retrieve blob size.";
        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errMsg));

        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionIndex = _actionIndexd
        });
        var errUrl = $"{_blobStreamerUrl.AbsoluteUri}blob/rangeError?deviceId={_deviceId}&fileName={_fileName}&actionIndex={_actionIndexd}&error={errMsg}";
        _httpRequestorServiceMock.Verify(service =>
               service.SendRequest(errUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
               Times.Once);
    }


}
