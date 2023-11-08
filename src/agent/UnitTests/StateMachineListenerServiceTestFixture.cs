using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Sevices;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class StateMachineListenerServiceTestFixture
{

    private Mock<IStateMachineChangedEvent> _stateMachineChangedEventMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private StateMachineListenerService _target;
    private Mock<ITwinHandler> _twinHandlerMock;
    private Mock<IC2DEventSubscriptionSession> _c2DEventSubscriptionSessionMock;

    [SetUp]
    public void Setup()
    {
        _stateMachineChangedEventMock = new Mock<IStateMachineChangedEvent>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _twinHandlerMock = new Mock<ITwinHandler>();
        _c2DEventSubscriptionSessionMock = new Mock<IC2DEventSubscriptionSession>();

        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var serviceScope = new Mock<IServiceScope>();
        _serviceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactory.Object);
        serviceScopeFactory.Setup(s => s.CreateScope()).Returns(serviceScope.Object);

        serviceScope.Setup(s => s.ServiceProvider.GetService(typeof(IC2DEventSubscriptionSession))).Returns(_c2DEventSubscriptionSessionMock.Object);
        serviceScope.Setup(s => s.ServiceProvider.GetService(typeof(ITwinHandler))).Returns(_twinHandlerMock.Object);

        _target = new StateMachineListenerService(_stateMachineChangedEventMock.Object, _serviceProviderMock.Object);
    }

    [Test]
    public void Constructor_NullParameters_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new StateMachineListenerService(_stateMachineChangedEventMock.Object, null));
    }

    [Test]
    public void HandleStateChangedEvent_ReadyState_C2dSubscribe()
    {
        _target.HandleStateChangedEvent(null, new StateMachineEventArgs(DeviceStateType.Ready));
        _c2DEventSubscriptionSessionMock.Verify(h => h.ReceiveC2DMessagesAsync(It.IsAny<CancellationToken>(), false), Times.Once);

    }

    [Test]
    public void HandleStateChangedEvent_ReadyState_TwinSubscribe()
    {
        _target.HandleStateChangedEvent(null, new StateMachineEventArgs(DeviceStateType.Ready));
        _twinHandlerMock.Verify(h => h.HandleTwinActionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleStateChangedEvent_ProvisioningState_C2dSubscribe()
    {
        _target.HandleStateChangedEvent(null, new StateMachineEventArgs(DeviceStateType.Provisioning));
        _c2DEventSubscriptionSessionMock.Verify(h => h.ReceiveC2DMessagesAsync(It.IsAny<CancellationToken>(), true), Times.Once);
    }

    

}