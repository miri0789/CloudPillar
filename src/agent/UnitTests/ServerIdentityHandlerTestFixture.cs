using CloudPillar.Agent.Handlers;
using Moq;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

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
            x509Certificate1 = MockHelper.GenerateCertificate("1", "", 60, CERTIFICATE_PREFIX);
            x509Certificate2 = MockHelper.GenerateCertificate("2", "", 60, CERTIFICATE_PREFIX);

            _target = new ServerIdentityHandler(_loggerMock.Object, _x509CertificateWrapper.Object, _fileStreamerWrapper.Object, _deviceClientWrapper.Object);
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

            await _target.HandleKnownIdentitiesFromCertificatesAsync(CancellationToken.None);

            _deviceClientWrapper.Verify(d => d.UpdateReportedPropertiesAsync(It.Is<string>(x => x == reportedKey),
             It.Is<List<KnownIdentities>>(y => EqualDetails(y, expected)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleKnownIdentitiesFromCertificatesAsync_NoCertificates_ExistFromFunction()
        {
            _fileStreamerWrapper.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { });
            await _target.HandleKnownIdentitiesFromCertificatesAsync(CancellationToken.None);

            _deviceClientWrapper.Verify(d => d.UpdateReportedPropertiesAsync(It.IsAny<string>(), It.IsAny<List<KnownIdentities>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task HandleKnownIdentitiesFromCertificatesAsync_CreateCrertificateFromFileException_ThrowException()
        {
            _fileStreamerWrapper.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(files);
            _x509CertificateWrapper.Setup(x => x.CreateFromFile(It.IsAny<string>())).Throws(new Exception());
            Assert.ThrowsAsync<Exception>(async () => await _target.HandleKnownIdentitiesFromCertificatesAsync(CancellationToken.None));
        }
        
        [Test]
        public async Task GetPublicKeyFromCertificate_GetRSAPublicKey_Success()
        {
            RSA rsa = RSA.Create();
            _x509CertificateWrapper.Setup(x => x.GetRSAPublicKey(x509Certificate1)).Returns(rsa);
            _x509CertificateWrapper.Setup(x => x.ExportSubjectPublicKeyInfo(It.IsAny<RSA>())).Returns("MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvzga8x+iIyrLferAnPzpIyuCO5PEKV3wgFaak94kDsm6W1qc7dxX4NrDZUT7cLqCIiv7qaszd+vQDzkQLJr24Fd1NAnOylnY1CIAMeSL7BWOhubBaWeMbVZT3j1ivFAT27DgkUnRH87KJbB/AUMRgsKbDsC6cKZmoaORfDv0so9NV7TDnaRcD6I2QiVRlFG3QMVFYZ2WyVBwbbElkARs0iLzv5+FU4VYw7Ht4LPxxZaxm5r6xhPjr9APsFGalEoLM0EH+RwzFpyLuaTI67JrN0pkX752+3a27XHuTMPFrVFyBNTstFZaAyW53E0eHegO/oNLpwzWFDlxQWRE6L3wMQIDAQAB");
            var publicKey = await _target.GetPublicKeyFromCertificate(x509Certificate1);

            var excepted = "-----BEGIN PUBLIC KEY-----\r\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvzga8x+iIyrLferAnPzp\r\nIyuCO5PEKV3wgFaak94kDsm6W1qc7dxX4NrDZUT7cLqCIiv7qaszd+vQDzkQLJr2\r\n4Fd1NAnOylnY1CIAMeSL7BWOhubBaWeMbVZT3j1ivFAT27DgkUnRH87KJbB/AUMR\r\ngsKbDsC6cKZmoaORfDv0so9NV7TDnaRcD6I2QiVRlFG3QMVFYZ2WyVBwbbElkARs\r\n0iLzv5+FU4VYw7Ht4LPxxZaxm5r6xhPjr9APsFGalEoLM0EH+RwzFpyLuaTI67Jr\r\nN0pkX752+3a27XHuTMPFrVFyBNTstFZaAyW53E0eHegO/oNLpwzWFDlxQWRE6L3w\r\nMQIDAQAB\r\n-----END PUBLIC KEY-----\r\n";

            Assert.AreEqual(excepted, publicKey);
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
