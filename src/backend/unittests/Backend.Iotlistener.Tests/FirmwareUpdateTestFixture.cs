using System.Reflection;
using common;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Entities.Events;
using Shared.Logger;

namespace Backend.Iotlistener.Tests;
public class FirmwareUpdateTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private FirmwareUpdateService _target;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private Uri _blobStreamerUrl;

    private const string _deviceId = "testDevice";
    private const string _fileName = "testFile.bin";
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
        _target = new FirmwareUpdateService(_httpRequestorServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);
    }

    private void SetMockBlobProperties(long blobSize)
    {
        var mockBlobProperties = new BlobProperties();
        typeof(BlobProperties).GetProperty("Length", BindingFlags.Public | BindingFlags.Instance)?.SetValue(mockBlobProperties, blobSize);

        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobProperties>(It.IsAny<string>(), HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockBlobProperties);
    }


    [Test]
    public async Task SendFirmwareUpdateAsync_SendsRangeRequests()
    {
        var actionGuid = new Guid();

        _httpRequestorServiceMock
                    .Setup(service => service.SendRequest(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()));

        SetMockBlobProperties(_blobSize);

        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });

        for (long offset = _startPosition, rangeIndex = 0; offset < _blobSize; offset += _rangeSize, rangeIndex++)
        {
            _httpRequestorServiceMock.Verify(service =>
                service.SendRequest($"{_blobStreamerUrl.AbsoluteUri}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}&actionGuid={actionGuid}&fileSize={_blobSize}", HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
    private string BuildBlobRangeUrl(Uri blobStreamerUrl, string deviceId, string fileName, int chunkSize, long rangeSize, Guid actionGuid)
    {
        return $"{blobStreamerUrl.AbsoluteUri}blob/range?deviceId={deviceId}&fileName={fileName}&chunkSize={chunkSize}&rangeSize={rangeSize}&rangeIndex=0&startPosition=0&actionGuid={actionGuid}&fileSize={rangeSize}";
    }

    [Test]
    public async Task SendFirmwareUpdateAsync_GetBlobSizeFails_ThrowsException()
    {
        var actionGuid = new Guid();

        _httpRequestorServiceMock
             .Setup(service => service.SendRequest<BlobProperties>(It.IsAny<string>(), HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new Exception("Failed to retrieve blob size."));

        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });
        string blobRangeUrl = BuildBlobRangeUrl(_blobStreamerUrl, _deviceId, _fileName, _chunkSize, _rangeSize, actionGuid);
        _httpRequestorServiceMock.Verify(service =>
           service.SendRequest(blobRangeUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
           Times.Never);
    }

    [Test]
    public async Task SendFirmwareUpdateAsync_RequestFails_ThrowsException()
    {
        var actionGuid = new Guid();

        _httpRequestorServiceMock.Setup(service =>
            service.SendRequest($"{_blobStreamerUrl.AbsoluteUri}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex=0&startPosition=0&actionGuid={actionGuid}&fileSize={_rangeSize}",
             HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to send request."));

        SetMockBlobProperties(_rangeSize);

        await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });

        _httpRequestorServiceMock.Verify(service =>
            service.SendRequest($"{_blobStreamerUrl.AbsoluteUri}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex=0&startPosition=0&actionGuid={actionGuid}&fileSize={_rangeSize}",
             HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

}
