﻿using System.Reflection;
using common;
using Microsoft.Azure.Storage.Blob;
using Moq;
using shared.Entities;

namespace iotlistener.tests;
public class FirmwareUpdateTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private FirmwareUpdateService _firmwareUpdateService;
    private Uri blobStreamerUrl;

    [SetUp]
    public void Setup()
    {
        blobStreamerUrl = new Uri("http://example.com/");
        Environment.SetEnvironmentVariable(Constants.blobStreamerUrl, blobStreamerUrl.AbsoluteUri);
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
    public async Task SendFirmwareUpdateAsync_SendsStartRangeRequestAndRangeRequests()
    {
        var deviceId = "testDevice";
        var fileName = "testFile.bin";
        var chunkSize = 1024;
        var startPosition = 0L;
        var rangeSize = 1024L;
        var blobSize = 2048L;
        var actionGuid = new Guid();

        _httpRequestorServiceMock
                    .Setup(service => service.SendRequest(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()));

        SetMockBlobProperties(blobSize);

        await _firmwareUpdateService.SendFirmwareUpdateAsync(deviceId, new FirmwareUpdateEvent
        {            
            FileName = fileName,
            ChunkSize = chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });

        for (long offset = startPosition, rangeIndex = 0; offset < blobSize; offset += rangeSize, rangeIndex++)
        {
            _httpRequestorServiceMock.Verify(service =>
                service.SendRequest($"{blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={fileName}&chunkSize={chunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}&actionGuid={actionGuid}", HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task SendFirmwareUpdateAsync_ThrowsException_WhenGetBlobSizeFails()
    {
        var deviceId = "testDevice";
        var fileName = "testFile.bin";
        var chunkSize = 1024;
        var rangeSize = 1024L;
        var actionGuid = new Guid();

        _httpRequestorServiceMock
             .Setup(service => service.SendRequest<BlobProperties>(It.IsAny<string>(), HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new Exception("Failed to retrieve blob size."));


        async Task SendFirmwareUpdate() => await _firmwareUpdateService.SendFirmwareUpdateAsync(deviceId, new FirmwareUpdateEvent
        {
            FileName = fileName,
            ChunkSize = chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });
        Assert.ThrowsAsync<Exception>(SendFirmwareUpdate);

        _httpRequestorServiceMock.Verify(service =>
            service.SendRequest($"{blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={fileName}&chunkSize={chunkSize}&rangeSize={rangeSize}&rangeIndex=0&startPosition=0&actionGuid={actionGuid}", HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SendFirmwareUpdateAsync_ThrowsException_WhenRequestFails()
    {
        var deviceId = "testDevice";
        var fileName = "testFile.bin";
        var chunkSize = 1024;
        var rangeSize = 1024L;
        var actionGuid = new Guid();

        _httpRequestorServiceMock.Setup(service =>
            service.SendRequest($"{blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={fileName}&chunkSize={chunkSize}&rangeSize={rangeSize}&rangeIndex=0&startPosition=0&actionGuid={actionGuid}", HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to send request."));

        SetMockBlobProperties(rangeSize);

        async Task SendFirmwareUpdate() => await _firmwareUpdateService.SendFirmwareUpdateAsync(deviceId, new FirmwareUpdateEvent
        {
            FileName = fileName,
            ChunkSize = chunkSize,
            StartPosition = 0,
            ActionGuid = actionGuid
        });
        Assert.ThrowsAsync<Exception>(SendFirmwareUpdate);
    }

}
