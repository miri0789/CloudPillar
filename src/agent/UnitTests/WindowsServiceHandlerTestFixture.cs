using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Utilities;
using Moq;

namespace UnitTests
{
    [TestFixture]
    public class WindowsServiceHandlerTestFixture
    {
        private Mock<ILoggerHandler> _loggerHandlerMock;
        private Mock<IWindowsServiceUtils> _windowsServiceUtils;
        private IWindowsServiceHandler _target;
        private const string SERVICE_NAME = "CloudPillar.Agent";

        [SetUp]
        public void Setup()
        {
            _loggerHandlerMock = new Mock<ILoggerHandler>();
            _windowsServiceUtils = new Mock<IWindowsServiceUtils>();
            _target = new WindowsServiceHandler(_windowsServiceUtils.Object, _loggerHandlerMock.Object);
        }

        [Test]
        public void InstallWindowsService_ServiceExistsAndRunning_StopService()
        {
            // Arrange
            _windowsServiceUtils.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            // Assert
            _windowsServiceUtils.Verify(x => x.StopService(SERVICE_NAME), Times.Once);
        }

        [Test]
        public void InstallWindowsService_ServiceExistsAndRunning_DeleteService()
        {
            // Arrange
            _windowsServiceUtils.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            // Assert
            _windowsServiceUtils.Verify(x => x.DeleteExistingService(SERVICE_NAME), Times.Once);
        }

        [Test]
        public void InstallWindowsService_ServiceExistsAndRunning_CreateService()
        {
            // Arrange
            _windowsServiceUtils.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            // Assert
            _windowsServiceUtils.Verify(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }


        [Test]
        public void InstallWindowsService_ServiceExistsAndNotRunning_DeleteService()
        {
            // Arrange
            _windowsServiceUtils.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(false);
            _windowsServiceUtils.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            // Assert
            _windowsServiceUtils.Verify(x => x.StopService(SERVICE_NAME), Times.Never);
        }

        [Test]
        public void InstallWindowsService_ServiceDoesNotExist_CreateService()
        {
            // Arrange
            _windowsServiceUtils.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(false);
            _windowsServiceUtils.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(false);
            _windowsServiceUtils.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            // Assert
            _windowsServiceUtils.Verify(x => x.DeleteExistingService(SERVICE_NAME), Times.Never);
        }

        [Test]
        public void InstallWindowsService_ServiceExistsAndRunning_DeleteService_ThrowsException()
        {
            // Arrange
            _windowsServiceUtils.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceUtils.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(false);
            _windowsServiceUtils.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(false);
            _windowsServiceUtils.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            Assert.ThrowsAsync<Exception>(async () => _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }
    }
}