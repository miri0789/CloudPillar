using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Shared;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class TwinHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<IFileDownloadHandler> _fileDownloadHandlerMock;
    private Mock<IFileUploaderHandler> _fileUploaderHandler;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private ITwinHandler _target;

    private string _baseDesierd = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";
    private string _baseReported = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";

    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _fileDownloadHandlerMock = new Mock<IFileDownloadHandler>();
        _fileUploaderHandler = new Mock<IFileUploaderHandler>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _target = new TwinHandler(_deviceClientMock.Object,
          _fileDownloadHandlerMock.Object,
          _fileUploaderHandler.Object,
          _loggerHandlerMock.Object);
    }

    [Test]
    public async Task GetTwinJsonAsync_ValidTwin_ReturnJson()
    {
        var twinProp = new TwinProperties();
        twinProp.Desired = new TwinCollection(_baseDesierd);
        twinProp.Reported = new TwinCollection(_baseReported);
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

    public Twin GetTwin(TwinChangeSpec desired, TwinReportedChangeSpec reported)
    {
        var desiredStr = JObject.Parse(_baseDesierd);
        desiredStr.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinDesired()
        {
            ChangeSpec = desired
        })));
        var reportedStr = JObject.Parse(_baseReported);
        reportedStr.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinReported()
        {
            ChangeSpec = reported
        })));

        var twinProp = new TwinProperties()
        {
            Desired = new TwinCollection(desiredStr.ToString(Formatting.Indented)),
            Reported = new TwinCollection(reportedStr.ToString(Formatting.Indented))
        };
        return new Twin(twinProp);
    }

    [Test]
    public async Task HandleTwinActionsAsync_NoNewActions_NotUpdateReport()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { ActionId = "123"},
                        new TwinAction() { ActionId = "456"}
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec()
        {
            Id = "123",
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray()
            }
        };

        var twin = GetTwin(desired, reported);
        _deviceClientMock.Setup(dc => dc.GetTwinAsync()).ReturnsAsync(twin);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>()));

        _target.HandleTwinActionsAsync(CancellationToken.None);
        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task HandleTwinActionsAsync_NewDesiredId_ExecuteAllActions()
    {
        var desired = new TwinChangeSpec()
        {
            Id = "123",
            Patch = new TwinPatch()
            {
                InstallSteps = new List<TwinAction>()
                    {   new TwinAction() { ActionId = "123"},
                        new TwinAction() { ActionId = "456"}
                    }.ToArray()
            }
        };

        var reported = new TwinReportedChangeSpec()
        {
            Id = "456",
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>()
                    {   new TwinActionReported() {Status = StatusType.Failed },
                        new TwinActionReported() {Status = StatusType.Success}
                    }.ToArray()
            }
        };

        var twin = GetTwin(desired, reported);
        _deviceClientMock.Setup(dc => dc.GetTwinAsync()).ReturnsAsync(twin);
        _deviceClientMock.Setup(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>()));

        _target.HandleTwinActionsAsync(CancellationToken.None);
        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}