using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using Moq;

namespace UnitTests
{
    [TestFixture]
    public class WindowsServiceHandlerTestFixture
    {
        private Mock<ILoggerHandler> _loggerHandlerMock;
        private Mock<IWindowsServiceWrapper> _windowsServiceWrapper;
        private IWindowsServiceHandler _target;
        private const string SERVICE_NAME = "CloudPillar.Agent";

        [SetUp]
        public void Setup()
        {
            _loggerHandlerMock = new Mock<ILoggerHandler>();
            _windowsServiceWrapper = new Mock<IWindowsServiceWrapper>();
            _target = new WindowsServiceHandler(_windowsServiceWrapper.Object, _loggerHandlerMock.Object);
        }

        [Test]
        public void InstallWindowsService_ServiceExistsAndRunning_StopAndDeleteService()
        {
            // Arrange
            _windowsServiceWrapper.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>());

            // Assert
            _windowsServiceWrapper.Verify(x => x.ServiceExists(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.IsServiceRunning(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.StopService(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.DeleteExistingService(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void InstallWindowsService_ServiceExistsAndNotRunning_DeleteService()
        {
            // Arrange
            _windowsServiceWrapper.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(false);
            _windowsServiceWrapper.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>());

            // Assert
            _windowsServiceWrapper.Verify(x => x.ServiceExists(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.IsServiceRunning(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.StopService(SERVICE_NAME), Times.Never);
            _windowsServiceWrapper.Verify(x => x.DeleteExistingService(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void InstallWindowsService_ServiceDoesNotExist_CreateService()
        {
            // Arrange
            _windowsServiceWrapper.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(false);
            _windowsServiceWrapper.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(false);
            _windowsServiceWrapper.Setup(x => x.StopService(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>())).Returns(true);

            // Act
            _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>());

            // Assert
            _windowsServiceWrapper.Verify(x => x.ServiceExists(SERVICE_NAME), Times.Once);
            _windowsServiceWrapper.Verify(x => x.IsServiceRunning(SERVICE_NAME), Times.Never);
            _windowsServiceWrapper.Verify(x => x.StopService(SERVICE_NAME), Times.Never);
            _windowsServiceWrapper.Verify(x => x.DeleteExistingService(SERVICE_NAME), Times.Never);
            _windowsServiceWrapper.Verify(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void InstallWindowsService_ServiceExistsAndRunning_DeleteService_ThrowsException()
        {
            // Arrange
            _windowsServiceWrapper.Setup(x => x.ServiceExists(SERVICE_NAME)).Returns(true);
            _windowsServiceWrapper.Setup(x => x.IsServiceRunning(SERVICE_NAME)).Returns(false);
            _windowsServiceWrapper.Setup(x => x.DeleteExistingService(SERVICE_NAME)).Returns(false);
            _windowsServiceWrapper.Setup(x => x.CreateAndStartService(SERVICE_NAME, It.IsAny<string>())).Returns(true);

            Assert.ThrowsAsync<Exception>(async () => _target.InstallWindowsService(SERVICE_NAME, It.IsAny<string>()));
        }
    }
}