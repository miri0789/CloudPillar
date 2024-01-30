using CloudPillar.Agent.Handlers;
using Moq;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using CloudPillar.Agent.Entities;
using Microsoft.Extensions.Options;
using System.Text;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class ServerIdentityHandlerTestFixture
    {
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<IX509CertificateWrapper> _x509CertificateWrapper;
        private Mock<IFileStreamerWrapper> _fileStreamerWrapper;
        private Mock<IDeviceClientWrapper> _deviceClientWrapper;
        private IServerIdentityHandler _target;
        private AppSettings appSettings = new AppSettings() { DefaultPublicKeyName = "UT-PublicKey" };
        private Mock<IOptions<AppSettings>> mockAppSettings;

        private const string CERTIFICATE_PREFIX = "UT_PREFIX";
        private string reportedKey = nameof(TwinReported.KnownIdentities);

        X509Certificate2 x509Certificate1;
        X509Certificate2 x509Certificate2;
        string[] files = new string[] { "certificate1.cer", "certificate2.cer" };

        public ServerIdentityHandlerTestFixture()
        {

            _loggerMock = new Mock<ILoggerHandler>();
            _x509CertificateWrapper = new Mock<IX509CertificateWrapper>();
            _fileStreamerWrapper = new Mock<IFileStreamerWrapper>();
            _deviceClientWrapper = new Mock<IDeviceClientWrapper>();
            mockAppSettings = new Mock<IOptions<AppSettings>>();
            mockAppSettings.Setup(ap => ap.Value).Returns(appSettings);

            x509Certificate1 = MockHelper.GenerateCertificate("1", "", 60, CERTIFICATE_PREFIX);
            x509Certificate2 = MockHelper.GenerateCertificate("2", "", 60, CERTIFICATE_PREFIX);

            CreateTrarget();
        }

        private void CreateTrarget()
        {
            _target = new ServerIdentityHandler(_loggerMock.Object, _x509CertificateWrapper.Object, _fileStreamerWrapper.Object, _deviceClientWrapper.Object, mockAppSettings.Object);
        }
        [Test]
        public async Task HandleKnownIdentitiesFromCertificatesAsync_ValidCertificates_ReturnsKnownIdentities()
        {
            _fileStreamerWrapper.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(files);
            var expected = new List<KnownIdentities>()
                    {
                        new KnownIdentities("CN=UT_PREFIX1", x509Certificate1.Thumbprint, x509Certificate1.NotAfter.ToString("yyyy-MM-dd HH:mm:ss")),
                        new KnownIdentities("CN=UT_PREFIX2", x509Certificate2.Thumbprint, x509Certificate2.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"))
                    };

            _x509CertificateWrapper.Setup(x => x.CreateFromFile("certificate1.cer")).Returns(x509Certificate1);
            _x509CertificateWrapper.Setup(x => x.CreateFromFile("certificate2.cer")).Returns(x509Certificate2);

            await _target.UpdateKnownIdentitiesFromCertificatesAsync(CancellationToken.None);

            _deviceClientWrapper.Verify(d => d.UpdateReportedPropertiesAsync(It.Is<string>(x => x == reportedKey),
             It.Is<List<KnownIdentities>>(y => EqualDetails(y, expected)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleKnownIdentitiesFromCertificatesAsync_CreateCrertificateFromFileException_ThrowException()
        {
            _fileStreamerWrapper.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(files);
            _x509CertificateWrapper.Setup(x => x.CreateFromFile(It.IsAny<string>())).Throws(new Exception());
            Assert.ThrowsAsync<Exception>(async () => await _target.UpdateKnownIdentitiesFromCertificatesAsync(CancellationToken.None));
        }

        [Test]
        public async Task GetPublicKeyFromCertificate_GetRSAPublicKey_Success()
        {

            _x509CertificateWrapper.Setup(x => x.CreateFromFile("certificate1.cer")).Returns(x509Certificate1);
            _x509CertificateWrapper.Setup(x => x.GetAlgorithmFriendlyName(x509Certificate1)).Returns("RSA");
            string myString = "Hello, World!";
            byte[] byteArray = Encoding.UTF8.GetBytes(myString);
            string base64String = Convert.ToBase64String(byteArray);
            _x509CertificateWrapper.Setup(x => x.ExportSubjectPublicKeyInfo(x509Certificate1)).Returns(byteArray);
            var publicKey = await _target.GetPublicKeyFromCertificateFileAsync("certificate1.cer");

            var excepted = "-----BEGIN RSA PUBLIC KEY-----\r\nSGVsbG8sIFdvcmxkIQ==\r\n-----END RSA PUBLIC KEY-----\r\n";

            Assert.AreEqual(excepted, publicKey);
        }

        [Test]
        public async Task RemoveNonDefaultCertificates_ValidProcess_RemoveAllNonDefaultCertificate()
        {
            files = new string[] { "certificate1.cer", "certificate2.cer", "UT-PublicKey.cer" };
            _fileStreamerWrapper.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(files);
            CreateTrarget();

            foreach (var file in files)
            {
                _fileStreamerWrapper.Setup(f => f.GetFileNameWithoutExtension(file)).Returns(Path.GetFileNameWithoutExtension(file));
            }

            await _target.RemoveNonDefaultCertificates("pki");
            _fileStreamerWrapper.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }

        private bool EqualDetails(List<KnownIdentities> current, List<KnownIdentities> expected)
        {
            if (current.Count != expected.Count)
            {
                return false;
            }
            for (int i = 0; i < current.Count; i++)
            {
                var isEqual = current[i].Subject == expected[i].Subject && current[i].Thumbprint == expected[i].Thumbprint && current[i].ValidThru == expected[i].ValidThru;
                if (!isEqual)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
