using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class StateMachineHandlerTestFixture
{
    private StateMachineHandler _target;
    private Mock<ITwinHandler> _twinHandler;
    private Mock<ILoggerHandler> _logger;
    private Mock<IStateMachineChangedEvent> _stateMachineChangedEventMock;


    [SetUp]
    public void Setup()
    {
        _twinHandler = new Mock<ITwinHandler>();
        _logger = new Mock<ILoggerHandler>();
        _stateMachineChangedEventMock = new Mock<IStateMachineChangedEvent>();


        _target = new StateMachineHandler(
            _twinHandler.Object,
            _stateMachineChangedEventMock.Object,
            _logger.Object
        );
    }

    [Test]
    public async Task GetStateAsync_ValidState_ReturnsState()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        var state = await _target.GetStateAsync();

        Assert.AreEqual(DeviceStateType.Ready, state);
    }


    [Test]
    public async Task GetStateAsync_NullState_ReturnsUninitialized()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync((DeviceStateType?)null);

        var state = await _target.GetStateAsync();

        Assert.AreEqual(DeviceStateType.Uninitialized, state);
    }

    [Test]
    public async Task GetStateAsync_Failure_ThrowException()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ThrowsAsync(new Exception());

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _target.GetStateAsync();
        });

    }

    [Test]
    public async Task InitStateMachineHandlerAsync_ReadyInitial_SetStaeteChanged()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        await _target.InitStateMachineHandlerAsync();

        _stateMachineChangedEventMock.Verify(h => h.SetStateChanged(It.IsAny<StateMachineEventArgs>()), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_ValidState_UpdateTwinState()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Busy);
        _twinHandler.Setup(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready, It.IsAny<CancellationToken>()));

        await _target.SetStateAsync(DeviceStateType.Ready, default);

        _twinHandler.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_SameState_NotUpdateState()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _twinHandler.Setup(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready, It.IsAny<CancellationToken>()));

        await _target.SetStateAsync(DeviceStateType.Ready, default);

        _twinHandler.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetStateAsync_AnyState_SaveStaticState()
    {
        var newState = DeviceStateType.Busy;

        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        await _target.SetStateAsync(DeviceStateType.Busy, default);

        var updatedState = _target.GetCurrentDeviceState();
        Assert.AreEqual(newState, updatedState);
    }


    [Test]
    public async Task SetStateAsync_BusyState_SaveLatestTwin()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        await _target.SetStateAsync(DeviceStateType.Busy, default);

        _twinHandler.Verify(x => x.SaveLastTwinAsync(CancellationToken.None), Times.Once);
    }

}
