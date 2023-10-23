using Moq;
using CloudPillar.Agent.Handlers;
using Shared.Logger;
using Shared.Entities.Twin;

[TestFixture]
public class StateMachineHandlerTestFixture
{
    private StateMachineHandler _target;
    private Mock<ITwinHandler> _twinHandler;
    private Mock<ILoggerHandler> _logger;
    private Mock<IC2DEventHandler> _c2DEventHandler;
    private Mock<IStateMachineTokenHandler> _stateMachineTokenHandler;

    [SetUp]
    public void Setup()
    {
        _twinHandler = new Mock<ITwinHandler>();
        _logger = new Mock<ILoggerHandler>();
        _c2DEventHandler = new Mock<IC2DEventHandler>();
        _stateMachineTokenHandler = new Mock<IStateMachineTokenHandler>();

        _target = new StateMachineHandler(
            _twinHandler.Object,
            _logger.Object,
            _c2DEventHandler.Object,
            _stateMachineTokenHandler.Object
        );
    }

    [Test]
    public async Task InitStateMachineHandlerAsync_ReadyInitial_StartToken()
    {
        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _stateMachineTokenHandler.Setup(h => h.StartToken()).Returns(new CancellationTokenSource());

        await _target.InitStateMachineHandlerAsync();

        _stateMachineTokenHandler.Verify(h => h.StartToken(), Times.Once);
    }

    // [Test]
    // public async Task SetStateAsync_UpdatesStateIfDifferent()
    // {
    //     _twinHandler.Setup(h => h.GetDeviceStateAsync()).ReturnsAsync(DeviceStateType.Ready);
    //     _twinHandler.Setup(h => h.UpdateDeviceStateAsync(DeviceStateType.Provisioning)).Returns(Task.CompletedTask);
    //     _c2DEventHandler.Setup(h => h.CreateProvisioningSubscribe(It.IsAny<CancellationToken>())).ReturnsAsync(true);

    //     await _target.SetStateAsync(DeviceStateType.Provisioning);

    //     _twinHandler.Verify(h => h.GetDeviceStateAsync(), Times.Once);
    //     _twinHandler.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Provisioning), Times.Once);
    //     _c2DEventHandler.Verify(h => h.CreateProvisioningSubscribe(It.IsAny<CancellationToken>()), Times.Once);
    //     _c2DEventHandler.Verify(h => h.CreateSubscribe(It.IsAny<CancellationToken>()), Times.Never);
    //     _logger.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
    // }

    [Test]
    public async Task SetStateAsync_SameState_NotUpdate()
    {
        await _target.SetStateAsync(DeviceStateType.Ready);

        _twinHandler.Setup(h => h.GetDeviceStateAsync(default)).ReturnsAsync(DeviceStateType.Ready);
        _twinHandler.Setup(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready));

        await _target.SetStateAsync(DeviceStateType.Ready);

        _twinHandler.Verify(h => h.UpdateDeviceStateAsync(DeviceStateType.Ready), Times.Never);
    }
//     [Test]
// public async Task GetStateAsync_ReturnsUninitializedIfTwinHandlerReturnsNull()
// {
//     _twinHandler.Setup(h => h.GetDeviceStateAsync()).ReturnsAsync((DeviceStateType)null);

//     var state = await _target.GetStateAsync();

//     Assert.AreEqual(DeviceStateType.Uninitialized, state);
//     _twinHandler.Verify(h => h.GetDeviceStateAsync(), Times.Once);
// }

// [Test]
// public async Task SetProvisioningAsync_SetsProvisioningState()
// {
//     _stateMachineTokenHandler.Setup(h => h.StartToken()).Returns(new CancellationTokenSource());
//     _c2DEventHandler.Setup(h => h.CreateProvisioningSubscribe(It.IsAny<CancellationToken>())).ReturnsAsync(true);
//     _stateMachineTokenHandler.Setup(h => h.CancelToken());

//     await _target.SetStateAsync(DeviceStateType.Provisioning);

//     _stateMachineTokenHandler.Verify(h => h.StartToken(), Times.Once);
//     _c2DEventHandler.Verify(h => h.CreateProvisioningSubscribe(It.IsAny<CancellationToken>()), Times.Once);
//     _stateMachineTokenHandler.Verify(h => h.CancelToken(), Times.Once);
// }

// [Test]
// public async Task SetReadyAsync_SetsReadyState()
// {
//     _stateMachineTokenHandler.Setup(h => h.StartToken()).Returns(new CancellationTokenSource());
//     _c2DEventHandler.Setup(h => h.CreateSubscribe(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
//     _twinHandler.Setup(h => h.HandleTwinActionsAsync(It.IsAny<CancellationToken>()));
//     _stateMachineTokenHandler.Setup(h => h.CancelToken());

//     await _target.SetStateAsync(DeviceStateType.Ready);

//     _stateMachineTokenHandler.Verify(h => h.StartToken(), Times.Once);
//     _c2DEventHandler.Verify(h => h.CreateSubscribe(It.IsAny<CancellationToken>()), Times.Once);
//     _twinHandler.Verify(h => h.HandleTwinActionsAsync(It.IsAny<CancellationToken>()), Times.Once);
//     _stateMachineTokenHandler.Verify(h => h.CancelToken(), Times.Once);
// }

// [Test]
// public void SetBusy_SetsBusyState()
// {
//     _stateMachineTokenHandler.Setup(h => h.CancelToken());

//     _target.SetStateAsync(DeviceStateType.Busy);

//     _stateMachineTokenHandler.Verify(h => h.CancelToken(), Times.Once);
// }

}
