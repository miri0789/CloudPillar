using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Shared;
using Moq;
using Newtonsoft.Json.Linq;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class TwinActionsHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private ITwinActionsHandler _target;

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

        Twin twin = MockHelper.CreateTwinMock(new TwinChangeSpec(), new TwinReportedChangeSpec()
        {
            Patch = new TwinReportedPatch()
            {
                InstallSteps = new List<TwinActionReported>() { new TwinActionReported() { } }.ToArray()
            }
        });
        _deviceClientMock.Setup(dc => dc.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);

        return actionsToReported;
    }

}