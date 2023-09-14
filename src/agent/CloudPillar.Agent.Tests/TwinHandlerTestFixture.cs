using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Shared;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class TwinHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<IFileDownloadHandler> _fileDownloadHandlerMock;
    private Mock<IFileUploaderHandler> _fileUploaderHandlerMock;
    private Mock<ILoggerHandler> _loggerMock;
    private ITwinHandler _target;

    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _fileDownloadHandlerMock = new Mock<IFileDownloadHandler>();
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _loggerMock = new Mock<ILoggerHandler>();

        _target = new TwinHandler(_deviceClientMock.Object, _fileDownloadHandlerMock.Object,_fileUploaderHandlerMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task GetTwinJsonAsync_ValidTwin_ReturnJson()
    {
        var desierd = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1
        }";
        var reported = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1
        }";
        var twinProp = new TwinProperties();
        twinProp.Desired = new TwinCollection(desierd);
        twinProp.Reported = new TwinCollection(reported);
        var twin = new Twin(twinProp);

        _deviceClientMock.Setup(x => x.GetTwinAsync()).ReturnsAsync(twin);

        var target = await _target.GetTwinJsonAsync();

        Assert.IsTrue(IsValidJson(target));
    }
    private bool IsValidJson(string str)
    {
        try
        {
            JToken.Parse(str);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }
}