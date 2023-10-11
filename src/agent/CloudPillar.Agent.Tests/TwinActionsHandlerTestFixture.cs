using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Shared;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class TwinActionsHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private ITwinActionsHandler _target;
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

    private CancellationToken cancellationToken = CancellationToken.None;

    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();

        _target = new TwinActionsHandler(_deviceClientMock.Object, _loggerHandlerMock.Object);
    }


    [Test]
    public async Task UpdateReportActionAsync_ValidReport_CallToUpdateReport()
    {
        var actionsToReported = CreateReportForUpdating();

        await _target.UpdateReportActionAsync(actionsToReported, cancellationToken);

        _deviceClientMock.Verify(dc => dc.UpdateReportedPropertiesAsync(
            nameof(TwinReported.ChangeSpec), It.IsAny<JObject>()), Times.Once);
    }

    private List<ActionToReport> CreateReportForUpdating()
    {
        var actionsToReported = new List<ActionToReport> { new ActionToReport
        {
            ReportPartName = "InstallSteps",
            ReportIndex = 0,
            TwinReport = new TwinActionReported { Status = StatusType.InProgress }
        } };

        CreateTwinMock(new TwinChangeSpec(), new TwinReportedChangeSpec()
        {
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>() { new TwinActionReported() { } }.ToArray()
            }
        });

        return actionsToReported;
    }

    private void CreateTwinMock(TwinChangeSpec desired, TwinReportedChangeSpec reported)
    {
        var desiredJson = JObject.Parse(_baseDesierd);
        desiredJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinDesired()
        {
            ChangeSpec = desired
        })));
        var reportedJson = JObject.Parse(_baseReported);
        reportedJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinReported()
        {
            ChangeSpec = reported
        })));
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        };
        var twinProp = new TwinProperties()
        {
            Desired = new TwinCollection(JsonConvert.SerializeObject(desiredJson, settings)),
            Reported = new TwinCollection(JsonConvert.SerializeObject(reportedJson, settings))
        };
        var twin = new Twin(twinProp);
        _deviceClientMock.Setup(dc => dc.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);
    }
}