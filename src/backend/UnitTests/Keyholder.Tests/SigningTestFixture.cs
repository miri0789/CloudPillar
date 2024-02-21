using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices;
using Moq;
using k8s;
using Microsoft.Azure.Devices.Shared;
using Backend.Keyholder.Interfaces;
using Shared.Logger;
using Backend.Keyholder.Services;
using Backend.Keyholder.Wrappers.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Shared.Entities.Twin;
using System.Security.Cryptography.X509Certificates;

namespace Backend.Keyholder.Tests;


public class SigningTestFixture
{
    private ISigningService _target;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLogger;
    private Mock<IRegistryManagerWrapper> _registryManagerWrapper;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapper;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapper;
    private string secretVolumeMountPath = "my-path";
    private string defaultSecretVolumeMountPath = "my-default-path";
    private const string PRIVATE_KEY_FILE = "tls.key";
    X509Certificate2 certificate;

    [SetUp]
    public void Setup()
    {
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLogger = new Mock<ILoggerHandler>();
        _registryManagerWrapper = new Mock<IRegistryManagerWrapper>();
        _x509CertificateWrapper = new Mock<IX509CertificateWrapper>();

        _mockEnvironmentsWrapper.Setup(c => c.iothubConnectionString).Returns("HostName=szlabs-iot-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dMBNypodzUSWPbxTXdWaV4PxJTR3jCwehPFCQn+XJXc=");
        _mockEnvironmentsWrapper.Setup(c => c.kubernetesServiceHost).Returns("your-kubernetes-service-host");
        _mockEnvironmentsWrapper.Setup(c => c.SecretVolumeMountPath).Returns(secretVolumeMountPath);
        _mockEnvironmentsWrapper.Setup(c => c.DefaultSecretVolumeMountPath).Returns(defaultSecretVolumeMountPath);
        _x509CertificateWrapper.Setup(mock => mock.CreateCertificateFrombytes(It.IsAny<byte[]>())).Returns(certificate);
        certificate = MockHelper.GenerateCertificate("subjectName");

        _fileStreamerWrapper = new Mock<IFileStreamerWrapper>();
        var registryManagerMock = new Mock<RegistryManager>();
        _registryManagerWrapper.Setup(mock => mock.CreateFromConnectionString())
                        .Returns(registryManagerMock.Object);
        _target = new SigningService(_mockEnvironmentsWrapper.Object, _mockLogger.Object, _registryManagerWrapper.Object, _fileStreamerWrapper.Object, _x509CertificateWrapper.Object);
    }


    private void InitPublicKeyFromEnvirementVar()
    {
        string publicCert = @"-----BEGIN CERTIFICATE-----
                            MIIDxTCCAq2gAwIBAgIBADANBgkqhk
                            -----END CERTIFICATE-----
                            -----BEGIN CERTIFICATE-----
                            MIIE0DCCA7igAwIBAgIBBzA
                            -----END CERTIFICATE-----
                            -----BEGIN CERTIFICATE-----
                            MIIGqz==
                            -----END CERTIFICATE-----";
        _fileStreamerWrapper.Setup(mock => mock.ReadAllTextAsync(It.IsAny<string>()))
                             .ReturnsAsync(publicCert);
    }

    private void InitPrivteKeyFromEnvirementVar()
    {
        string publicCert = @"-----BEGIN RSA PRIVATE KEY-----
            MIIEowIBAAKCAQEA1Z1rBlmzzrpTA34SonQTY2TINDGCKJs5P9fKH8/L1g
            -----END RSA PRIVATE KEY-----
            ";
        _fileStreamerWrapper.Setup(mock => mock.ReadAllTextAsync(It.IsAny<string>()))
                             .ReturnsAsync(publicCert);
    }

    [Test]
    public async Task GetSigningPublicKeyAsync_SecretVolumeMountPathIsNull_TrowException()
    {
        _mockEnvironmentsWrapper.Setup(c => c.SecretVolumeMountPath).Returns("");

        Assert.ThrowsAsync<InvalidOperationException>(() => _target.GetSigningPublicKeyAsync(), "default cert secret path must be set.");
    }

    [Test]
    public async Task GetSigningPublicKeyAsync_PublicKeyPemWithValue_GetLastCertificateSection()
    {
        InitPublicKeyFromEnvirementVar();
        _mockEnvironmentsWrapper.Setup(c => c.SecretVolumeMountPath).Returns(secretVolumeMountPath);

        var res = await _target.GetSigningPublicKeyAsync();
        var publicKeyPem = "-----BEGIN CERTIFICATE-----\r\n                            MIIGqz==\r\n                            -----END CERTIFICATE-----";
        Assert.AreEqual(Encoding.UTF8.GetBytes(publicKeyPem), res);
    }

    [Test]
    public async Task GetSigningPublicKeyAsync_PublicKeyPemIsEmpty_TrowException()
    {
        _fileStreamerWrapper.Setup(mock => mock.ReadAllTextAsync(It.IsAny<string>()))
                     .ReturnsAsync("");
        _mockEnvironmentsWrapper.Setup(c => c.SecretVolumeMountPath).Returns(secretVolumeMountPath);
        Assert.ThrowsAsync<InvalidOperationException>(() => _target.GetSigningPublicKeyAsync(), "public key is empty");
    }


    [Test]
    public async Task SignData_SecretVolumeMountPathIsNull_TrowException()
    {
        _mockEnvironmentsWrapper.Setup(c => c.SecretVolumeMountPath).Returns("");
        var res = await _target.SignData(new byte[] { 1, 2, 3 }, "my-device");
        Assert.AreEqual("", res);
    }

    [Test]
    public async Task SignData_KnownIdentitiesEmpty_GoToDefault()
    {
        SetRpoertedTwinWithKnownIdentities(new List<KnownIdentities> { });
        InitPrivteKeyFromEnvirementVar();
        await _target.SignData(new byte[] { 1, 2, 3 }, "my-device");
        var defaultPath = Path.Combine(defaultSecretVolumeMountPath, PRIVATE_KEY_FILE);
        _fileStreamerWrapper.Verify(mock => mock.OpenRead(It.Is<string>(x => x == defaultPath)), Times.Once);
    }

    private void SetRpoertedTwinWithKnownIdentities(List<KnownIdentities> knownIdentities = null)
    {
        var twin = new Twin();
        twin.Properties.Reported["knownIdentities"] = JToken.FromObject(knownIdentities);
        _registryManagerWrapper.Setup(mock => mock.GetTwinAsync(It.IsAny<RegistryManager>(), It.IsAny<string>()))
                               .ReturnsAsync(twin);
    }

}
