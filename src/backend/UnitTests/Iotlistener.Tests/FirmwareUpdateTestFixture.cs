using System.Reflection;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Microsoft.Azure.Storage.Blob;
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
    private const string _actionId = "123";
    private const string _fileName = "testFile.bin";
    private const string _checkSum = "abcdef";
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
            .Setup(service => service.SendRequest<string>($"{_blobStreamerUrl.AbsoluteUri}blob/GetFileCheckSum?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_checkSum); 
        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobData(){Length = _blobSize});
        _target = new FirmwareUpdateService(_httpRequestorServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);
    }


    [Test]
    public async Task SendFirmwareUpdateAsync_ValidCheckSum_SndCheckSumToRequests()
    {

        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionId = _actionId
        });

        string blobRangeUrl = BuildBlobRangeUrl();
        _httpRequestorServiceMock.Verify(service =>
            service.SendRequest(blobRangeUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
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
            ActionId = _actionId
        });

        for (long offset = _startPosition, rangeIndex = 0; offset < _blobSize; offset += _rangeSize, rangeIndex++)
        {
            string blobRangeUrl = BuildBlobRangeUrl(rangeIndex, offset);
            _httpRequestorServiceMock.Verify(service =>
                service.SendRequest(blobRangeUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
    private string BuildBlobRangeUrl(long rangeIndex = 0, long startPosition = 0)
    {
        return $"{_blobStreamerUrl.AbsoluteUri}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex={rangeIndex}&startPosition={startPosition}&actionId={_actionId}&checkSum={_checkSum}";
    }



    [Test]
    public async Task SendFirmwareUpdateAsync_GetBlobSizeFails_ThrowsException()
    {

        _httpRequestorServiceMock
             .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new Exception("Failed to retrieve blob size."));

             
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent()));
    }



    [Test]
    public async Task SendFirmwareUpdateAsync_NullOrEmptyFileCheckSum_ThrowsArgumentNullException()
    {
        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<string>($"{_blobStreamerUrl.AbsoluteUri}blob/GetFileCheckSum?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(String.Empty);


        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent()));
    }
}
