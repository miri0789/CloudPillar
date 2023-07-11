using System.Reflection;
using common;
using iotlistener.Services;
using Microsoft.Azure.Storage.Blob;
using Moq;
using shared.Entities.Events;

namespace iotlistener.tests;
public class FirmwareUpdateTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private FirmwareUpdateService _firmwareUpdateService;
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
        _blobStreamerUrl = new Uri("http://example.com/");
        Environment.SetEnvironmentVariable(Constants.blobStreamerUrl, _blobStreamerUrl.AbsoluteUri);
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _firmwareUpdateService = new FirmwareUpdateService(_httpRequestorServiceMock.Object);
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

        await _firmwareUpdateService.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });

        for (long offset = _startPosition, rangeIndex = 0; offset < _blobSize; offset += _rangeSize, rangeIndex++)
        {
            _httpRequestorServiceMock.Verify(service =>
                service.SendRequest($"{_blobStreamerUrl}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}&actionGuid={actionGuid}&fileSize={_blobSize}", HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task SendFirmwareUpdateAsync_GetBlobSizeFails_ThrowsException()
    {
        var actionGuid = new Guid();

        _httpRequestorServiceMock
             .Setup(service => service.SendRequest<BlobProperties>(It.IsAny<string>(), HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new Exception("Failed to retrieve blob size."));


        async Task SendFirmwareUpdate() => await _firmwareUpdateService.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });
        Assert.ThrowsAsync<Exception>(SendFirmwareUpdate);

        _httpRequestorServiceMock.Verify(service =>
            service.SendRequest($"{_blobStreamerUrl}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex=0&startPosition=0&actionGuid={actionGuid}&fileSize={_rangeSize}", HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SendFirmwareUpdateAsync_RequestFails_ThrowsException()
    {
        var actionGuid = new Guid();

        _httpRequestorServiceMock.Setup(service =>
            service.SendRequest($"{_blobStreamerUrl}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex=0&startPosition=0&actionGuid={actionGuid}&fileSize={_rangeSize}", HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to send request."));

        SetMockBlobProperties(_rangeSize);

        async Task SendFirmwareUpdate() => await _firmwareUpdateService.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent
        {
            FileName = _fileName,
            ChunkSize = _chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });
        Assert.ThrowsAsync<Exception>(SendFirmwareUpdate);
    }

}
