using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class StateMachineHandlerTestFixture
{
    private StateMachineHandler _target;
    private Mock<ITwinHandler> _twinHandlerMock;
    private Mock<ITwinReportHandler> _twinReportHandlerMock;
    private Mock<ILoggerHandler> _logger;
    private Mock<IStateMachineChangedEvent> _stateMachineChangedEventMock;


    [SetUp]
    public void Setup()
    {
        _twinHandlerMock = new Mock<ITwinHandler>();
        _twinReportHandlerMock = new Mock<ITwinReportHandler>();
        _logger = new Mock<ILoggerHandler>();
        _stateMachineChangedEventMock = new Mock<IStateMachineChangedEvent>();


        _target = new StateMachineHandler(
            _twinHandlerMock.Object,
            _stateMachineChangedEventMock.Object,
            _logger.Object,
            _twinReportHandlerMock.Object
        );
    }

    [Test]
    public async Task GetStateAsync_ValidState_ReturnsState()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        var state = await _target.GetStateAsync();

        Assert.AreEqual(DeviceStateType.Ready, state);
    }


    [Test]
    public async Task GetStateAsync_NullState_ReturnsUninitialized()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync((DeviceStateType?)null);

        var state = await _target.GetStateAsync();

        Assert.AreEqual(DeviceStateType.Uninitialized, state);
    }

    [Test]
    public async Task GetStateAsync_Failure_ThrowException()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ThrowsAsync(new Exception());

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _target.GetStateAsync();
        });

    }

    [Test]
    public async Task InitStateMachineHandlerAsync_ReadyInitial_SetStaeteChanged()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        await _target.InitStateMachineHandlerAsync(default);

        _stateMachineChangedEventMock.Verify(h => h.SetStateChanged(It.IsAny<StateMachineEventArgs>()), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_ValidState_UpdateTwinState()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Busy);        

        await _target.SetStateAsync(DeviceStateType.Ready, default);

        _twinReportHandlerMock.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_SameState_NotUpdateState()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);        

        await _target.SetStateAsync(DeviceStateType.Ready, default);

        _twinReportHandlerMock.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetStateAsync_AnyState_SaveStaticState()
    {
        var newState = DeviceStateType.Busy;

        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        await _target.SetStateAsync(DeviceStateType.Busy, default);

        var updatedState = _target.GetCurrentDeviceState();
        Assert.AreEqual(newState, updatedState);
    }


    [Test]
    public async Task SetStateAsync_BusyState_SaveLatestTwin()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        await _target.SetStateAsync(DeviceStateType.Busy, default);

        _twinHandlerMock.Verify(x => x.SaveLastTwinAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetInitStateAsync_ValidState_ReturnsState()
    {
        _twinReportHandlerMock.Setup(th => th.GetDeviceStateAfterServiceRestartAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        var state = await _target.GetInitStateAsync();

        _twinReportHandlerMock.Verify(th => th.GetDeviceStateAfterServiceRestartAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.AreEqual(DeviceStateType.Ready, state);
    }

    [Test]
    public async Task GetStateAsync_SameStateInitDevice_SetStateChanged()
    {
        _twinReportHandlerMock.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

         await _target.SetStateAsync(DeviceStateType.Ready,CancellationToken.None, true);

        _stateMachineChangedEventMock.Verify(h => h.SetStateChanged(It.IsAny<StateMachineEventArgs>()), Times.Once);
    }
}
