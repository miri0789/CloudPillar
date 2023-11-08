using Moq;
using CloudPillar.Agent.Handlers;
using Shared.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;

[TestFixture]
public class StateMachineHandlerTestFixture
{
    private StateMachineHandler _target;
    private Mock<ITwinHandler> _twinHandler;
    private Mock<ILoggerHandler> _logger;
    private Mock<IC2DEventHandler> _c2DEventHandler;
    private Mock<IStateMachineTokenHandler> _stateMachineTokenHandler;
    private Mock<IDeviceClientWrapper> _deviceClientWrapper;

    [SetUp]
    public void Setup()
    {
        _twinHandler = new Mock<ITwinHandler>();
        _logger = new Mock<ILoggerHandler>();
        _c2DEventHandler = new Mock<IC2DEventHandler>();
        _stateMachineTokenHandler = new Mock<IStateMachineTokenHandler>();
        _deviceClientWrapper = new Mock<IDeviceClientWrapper>();

        _target = new StateMachineHandler(
            _twinHandler.Object,
            _logger.Object,
            _c2DEventHandler.Object,
            _stateMachineTokenHandler.Object,
            _deviceClientWrapper.Object
        );

        _stateMachineTokenHandler.Setup(h => h.StartToken()).Returns(new CancellationTokenSource());
        _c2DEventHandler.Setup(h => h.CreateSubscribeAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
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
    public async Task InitStateMachineHandlerAsync_ReadyInitial_StartToken()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);

        await _target.InitStateMachineHandlerAsync();

        _stateMachineTokenHandler.Verify(h => h.StartToken(), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_ValidState_UpdateTwinState()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Busy);
        _twinHandler.Setup(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready));

        await _target.SetStateAsync(DeviceStateType.Ready);

        _twinHandler.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_ReadyState_StartToken()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Busy);
        _stateMachineTokenHandler.Setup(h => h.StartToken()).Returns(new CancellationTokenSource());

        await _target.SetStateAsync(DeviceStateType.Ready);

        _stateMachineTokenHandler.Verify(h => h.StartToken(), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_ReadyState_C2dSubscribe()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Busy);
        await _target.SetStateAsync(DeviceStateType.Ready);

        _c2DEventHandler.Verify(h => h.CreateSubscribeAsync(It.IsAny<CancellationToken>(), false), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_ReadyState_HandleTwinActions()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Busy);
        await _target.SetStateAsync(DeviceStateType.Ready);

        _twinHandler.Verify(h => h.HandleTwinActionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_BusyState_CancelToken()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _stateMachineTokenHandler.Setup(h => h.CancelToken());

        await _target.SetStateAsync(DeviceStateType.Busy);

        _stateMachineTokenHandler.Verify(h => h.CancelToken(), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_BusyState_DisposeTwin()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _stateMachineTokenHandler.Setup(h => h.CancelToken());

        await _target.SetStateAsync(DeviceStateType.Busy);

        _deviceClientWrapper.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_AnyState_SaveStaticState()
    {
        var newState = DeviceStateType.Busy;

        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _stateMachineTokenHandler.Setup(h => h.CancelToken());

        await _target.SetStateAsync(DeviceStateType.Busy);

        var updatedState = _target.GetCurrentDeviceState();
        Assert.AreEqual(newState, updatedState);
    }

    [Test]
    public async Task SetStateAsync_BusyState_SaveLatestTwin()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _stateMachineTokenHandler.Setup(h => h.CancelToken());

        await _target.SetStateAsync(DeviceStateType.Busy);

        _twinHandler.Verify(x => x.SaveLastTwinAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_ProvisioningState_StartToken()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Busy);
        _stateMachineTokenHandler.Setup(h => h.StartToken()).Returns(new CancellationTokenSource());

        await _target.SetStateAsync(DeviceStateType.Provisioning);

        _stateMachineTokenHandler.Verify(h => h.StartToken(), Times.Once);
    }

    [Test]
    public async Task SetStateAsync_SameState_NotUpdateState()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _twinHandler.Setup(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready));

        await _target.SetStateAsync(DeviceStateType.Ready);

        _twinHandler.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready), Times.Never);
    }


}
