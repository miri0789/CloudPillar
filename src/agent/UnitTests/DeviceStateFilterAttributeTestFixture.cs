using CloudPillar.Agent.Controllers;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class DeviceStateFilterAttributeTestFixture
    {
        private const int NOT_READY_STATUS = 400;
        private const int SERVICE_UNAVAILABLE_STATUS = 503;
        private Mock<ITwinHandler> _twinHandler;
        private Mock<IValidator<UpdateReportedProps>> _updateReportedPropsValidator;
        private Mock<IDPSProvisioningDeviceClientHandler> _dPSProvisioningDeviceClientHandler;
        private Mock<ISymmetricKeyProvisioningHandler> _symmetricKeyProvisioningHandler;
        private Mock<IValidator<TwinDesired>> _twinDesiredPropsValidator;
        private Mock<IStateMachineHandler> _stateMachineHandler;
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<ActionExecutingContext> actionExecutingContextMock;
        private Mock<IRunDiagnosticsHandler> _runDiagnosticsHandler;
        private Mock<IStateMachineChangedEvent> _stateMachineChangedEventMock;
        private ActionExecutingContext _context;
        private AgentController agentController;
        private DeviceStateFilterAttribute _target;

        public DeviceStateFilterAttributeTestFixture()
        {

            _twinHandler = new Mock<ITwinHandler>();
            _updateReportedPropsValidator = new Mock<IValidator<UpdateReportedProps>>();
            _dPSProvisioningDeviceClientHandler = new Mock<IDPSProvisioningDeviceClientHandler>();
            _symmetricKeyProvisioningHandler = new Mock<ISymmetricKeyProvisioningHandler>();
            _twinDesiredPropsValidator = new Mock<IValidator<TwinDesired>>();
            _stateMachineHandler = new Mock<IStateMachineHandler>();
            _loggerMock = new Mock<ILoggerHandler>();
            _runDiagnosticsHandler = new Mock<IRunDiagnosticsHandler>();
            _stateMachineChangedEventMock = new Mock<IStateMachineChangedEvent>();
            actionExecutingContextMock = new Mock<ActionExecutingContext>();

            _target = new DeviceStateFilterAttribute();

            agentController = new AgentController(_twinHandler.Object, _updateReportedPropsValidator.Object, _dPSProvisioningDeviceClientHandler.Object,
            _symmetricKeyProvisioningHandler.Object, _twinDesiredPropsValidator.Object, _stateMachineHandler.Object, _runDiagnosticsHandler.Object, _loggerMock.Object, _stateMachineChangedEventMock.Object);


            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor(),
            };

            _context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                agentController);

        }

        [Test]
        public void OnActionExecuting_DeviceNotReady_Returns400StatusCode()
        {
            actionExecutingContextMock.SetupGet(x => x.Controller).Returns(agentController);

            _target.OnActionExecuting(_context);

            var result = _context.Result as ObjectResult;
            Assert.AreEqual(NOT_READY_STATUS, result.StatusCode);
        }

        [Test]
        public void OnActionExecuting_DeviceStateBusy_Returns503StatusCode()
        {
            _stateMachineHandler.Setup(x => x.GetCurrentDeviceState()).Returns(DeviceStateType.Busy);
            actionExecutingContextMock.SetupGet(x => x.Controller).Returns(agentController);

            _target.OnActionExecuting(_context);

            var result = _context.Result as ObjectResult;
            Assert.AreEqual(SERVICE_UNAVAILABLE_STATUS, result.StatusCode);
        }
    }
}
