using CloudPillar.Agent.Controllers;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using FluentValidation;
using Moq;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;
using Microsoft.AspNetCore.Http;
using CloudPillar.Agent.Sevices.Interfaces;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class AgentControllerTestFixture
    {
        private Mock<IStateMachineChangedEvent> _stateMachineChangedEventMock;
        private Mock<IReprovisioningHandler> _reprovisioningHandlerMock;
        private Mock<ITwinReportHandler> _twinReportHandlerMock;
        private Mock<ITwinHandler> _twinHandler;
        private Mock<IValidator<UpdateReportedProps>> _updateReportedPropsValidator;
        private Mock<IDPSProvisioningDeviceClientHandler> _dPSProvisioningDeviceClientHandler;
        private Mock<ISymmetricKeyProvisioningHandler> _symmetricKeyProvisioningHandler;
        private Mock<IValidator<TwinDesired>> _twinDesiredPropsValidator;
        private Mock<IStateMachineHandler> _stateMachineHandler;
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<IRunDiagnosticsHandler> _runDiagnosticsHandler;
        private Mock<IServerIdentityHandler> _serverIdentityHandlerMock;
        private Mock<IRequestWrapper> _requestWrapper;
        private Mock<IProvisioningService> _provisioningService;
        private AgentController _target;
        public AgentControllerTestFixture()
        {

            _twinHandler = new Mock<ITwinHandler>();
            _updateReportedPropsValidator = new Mock<IValidator<UpdateReportedProps>>();
            _dPSProvisioningDeviceClientHandler = new Mock<IDPSProvisioningDeviceClientHandler>();
            _symmetricKeyProvisioningHandler = new Mock<ISymmetricKeyProvisioningHandler>();
            _twinDesiredPropsValidator = new Mock<IValidator<TwinDesired>>();
            _stateMachineHandler = new Mock<IStateMachineHandler>();
            _runDiagnosticsHandler = new Mock<IRunDiagnosticsHandler>();
            _loggerMock = new Mock<ILoggerHandler>();
            _stateMachineChangedEventMock = new Mock<IStateMachineChangedEvent>();
            _reprovisioningHandlerMock = new Mock<IReprovisioningHandler>();
            _twinReportHandlerMock = new Mock<ITwinReportHandler>();
            _serverIdentityHandlerMock = new Mock<IServerIdentityHandler>();
            _requestWrapper = new Mock<IRequestWrapper>();
            _provisioningService = new Mock<IProvisioningService>();

            _target = new AgentController(_twinHandler.Object, _twinReportHandlerMock.Object, _updateReportedPropsValidator.Object, _dPSProvisioningDeviceClientHandler.Object,
                        _symmetricKeyProvisioningHandler.Object, _twinDesiredPropsValidator.Object, _stateMachineHandler.Object, _runDiagnosticsHandler.Object,
                         _loggerMock.Object, _stateMachineChangedEventMock.Object, _reprovisioningHandlerMock.Object, _serverIdentityHandlerMock.Object, _requestWrapper.Object,
                         _provisioningService.Object);
        }

        [Test]
        public async Task SetBusyAsync_ValidProccess_Success()
        {
            await _target.SetBusyAsync(default);
            _twinHandler.Verify(h => h.GetLatestTwin(), Times.Once);
        }

        [Test]
        public async Task RunDiagnostics_ProccessActive_BadRequest()
        {
            RunDiagnosticsHandler.IsDiagnosticsProcessRunning = true;
            await _target.RunDiagnostics();
            _runDiagnosticsHandler.Verify(x => x.HandleRunDiagnosticsProcess(It.IsAny<CancellationToken>()), Times.Never);
            RunDiagnosticsHandler.IsDiagnosticsProcessRunning = false;

        }

        [Test]
        public async Task InitiateProvisioningAsync_HandleProvisinigSymetricKeyAsync_Success()
        {
            var mockHeaders = new HeaderDictionary();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(x => x.HttpContext.Request.Headers).Returns(mockHeaders);
            await _target.InitiateProvisioningAsync(CancellationToken.None);
            _provisioningService.Verify(x => x.ProvisinigSymetricKeyAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
