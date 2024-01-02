using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests
{
    [TestFixture]
    public class WindowsServiceWrapperTestFixture
    {
        private Mock<IOptions<AuthenticationSettings>> _authenticationSettingsMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
        private Mock<IWindowsServiceWrapper> _target;
        private const string SERVICE_NAME = "CloudPillar.Agent";

        [SetUp]
        public void Setup()
        {
            _authenticationSettingsMock = new Mock<IOptions<AuthenticationSettings>>();
            _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { UserName = "UnitTest", UserPassword = "UnitTestPassword" });
            _loggerHandlerMock = new Mock<ILoggerHandler>();
            CreateTarget();
        }

        [Test]
        public void StartService_ValidServiceName_StartService()
        {
            _target.Setup(w => w.ServiceExists(SERVICE_NAME)).Returns(true);
            _target.Setup(w => w.InstallWindowsService(SERVICE_NAME, It.IsAny<string>()));
            _target.Verify(w => w.DeleteExistingService(SERVICE_NAME), Times.Once);
        }

        

        private void CreateTarget()
        {
            _target = new Mock<IWindowsServiceWrapper>();
        }

    }
}