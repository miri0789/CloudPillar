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
    }
    
    private string BuildBlobRangeUrl(long rangeIndex = 0, long startPosition = 0)
    {
        return $"{_blobStreamerUrl.AbsoluteUri}blob/range?deviceId={_deviceId}&fileName={_fileName}&chunkSize={_chunkSize}&rangeSize={_rangeSize}&rangeIndex={rangeIndex}&startPosition={startPosition}&actionId={_actionId}&rangesCount={_rangesCount}";
    }



    [Test]
    public async Task SendFirmwareUpdateAsync_GetBlobSizeFails_ThrowsException()
    {
        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<BlobData>($"{_blobStreamerUrl.AbsoluteUri}blob/metadata?fileName={_fileName}", HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to retrieve blob size."));

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.SendFirmwareUpdateAsync(_deviceId, new FirmwareUpdateEvent() { FileName = _fileName }));
    }


}
