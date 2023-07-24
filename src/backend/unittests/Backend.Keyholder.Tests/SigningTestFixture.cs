using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices;
using Moq;
using k8s;
using k8s.Models;
using Microsoft.Azure.Devices.Shared;
using Backend.Keyholder.Services;
using Shared.Logger;

namespace Backend.Keyholder.Tests;


public class SigningTestFixture
{
    private Mock<RegistryManager> _registryManagerMock;
    private Mock<ECDsa> _ecdsaMock;
    private SigningService _signingService;
    private Mock<Kubernetes> _kubernetesMock;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _registryManagerMock = new Mock<RegistryManager>();
        _ecdsaMock = new Mock<ECDsa>();
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLogger = new Mock<ILoggerHandler>();
        _mockEnvironmentsWrapper.Setup(c => c.iothubConnectionString).Returns("HostName=szlabs-iot-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dMBNypodzUSWPbxTXdWaV4PxJTR3jCwehPFCQn+XJXc=");
        _mockEnvironmentsWrapper.Setup(c => c.signingPem).Returns("");
        _mockEnvironmentsWrapper.Setup(c => c.kubernetesServiceHost).Returns("your-kubernetes-service-host");
        _mockEnvironmentsWrapper.Setup(c => c.secretName).Returns("");
        _mockEnvironmentsWrapper.Setup(c => c.secretKey).Returns("your-secret-key");

        _signingService = new SigningService(_mockEnvironmentsWrapper.Object, _mockLogger.Object);
        _signingService.GetType()
            .GetField("_registryManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_signingService, _registryManagerMock.Object);
        _signingService.GetType()
            .GetField("_signingPrivateKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_signingService, _ecdsaMock.Object);
        _kubernetesMock = new Mock<Kubernetes>();
    }

    [Test]
    public async Task GetSigningPrivateKeyAsync_Should_ThrowError_When_SecretNameOrSecretKeyIsNullOrEmpty()
    {
        async Task InitSigning() => await _signingService.Init();
        Assert.ThrowsAsync<InvalidOperationException>(InitSigning);
        _mockLogger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("In kube run-time - loading crypto from the secret in the local namespace."))), Times.Once);
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Private key secret name and secret key must be set."))), Times.Once);
    }

    [Test]
    public async Task GetSigningPrivateKeyAsync_ValidSecretNameAndSecretKey()
    {
        _mockEnvironmentsWrapper.Setup(c => c.secretName).Returns("your-secret-name");
        _mockEnvironmentsWrapper.Setup(c => c.secretKey).Returns("your-secret-key");

        async Task InitSigning() => await _signingService.Init();
        Assert.ThrowsAsync<InvalidOperationException>(InitSigning);
        _mockLogger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("In kube run-time - loading crypto from the secret in the local namespace."))), Times.Once);
        _mockLogger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Got k8s secret bytes"))), Times.Once);
    }

    [Test]
    public async Task GetSigningPrivateKeyAsync_NoK8sServiceHost()
    {
        _mockEnvironmentsWrapper.Setup(c => c.kubernetesServiceHost).Returns("");

        await _signingService.Init();
        _mockLogger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("Not in kube run-time - loading crypto from the local storage."))), Times.Once);
    }

    private void InitKeyFromEnvirementVar()
    {
        string pemContent = @"LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSHVBZ0VBTUJBR0J5cUdTTTQ5QWdFR0JTdUJCQUFqQklIV01JSFRBZ0VCQkVJQml5QWE3YVJIRkRDaDJxZ2EKOXNUVUdJTkU1akhBRm5tTTh4V2VUL3VuaTVJNHROcWhWNVh4MHBEcm1DVjltYnJvRnRmRWEwWFZmS3VNQXh4ZgpaNkxNL3lLaGdZa0RnWVlBQkFHQnpnZG5QNzk4RnNMdVdZVEREUUE3YzByM0JWazhOblJVU2V4cFFVc1JpbFBOCnYzU2NoTzBsUnc5UnU4Nngxa2huVkR4K2R1cTRCaURGY3ZsU0FjeWpMQUNKdmp2b3lUTEppQStUUUZkbXJlYXIKak1pWk5FMjVwVDJ5V1AxTlVuZEp4UGN2VnRmQlc0OGtQT212a1k0V2xxUDViQXdDWHdic0tyQ2drNnhic3AxMgpldz09Ci0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0=";
        _mockEnvironmentsWrapper.Setup(c => c.signingPem).Returns(pemContent);
    }

    [Test]
    public async Task LoadPrivateKeyFromEnvirementVariable_ValidPrivateKey_InitECDsaInstance()
    {
        InitKeyFromEnvirementVar();
        await _signingService.Init();
        Assert.IsNotNull(_ecdsaMock.Object);
        Assert.IsInstanceOf<ECDsa>(_ecdsaMock.Object);
        _mockLogger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("Key Base64 decoded layer 1"))), Times.Once);
    }

    [Test]
    public async Task CreateTwinKeySignature_ValidArguments_SignatureAddedToTwin()
    {
        var deviceId = "testDevice";
        var keyPath = "path";
        var signatureKey = "signatureKey";

        var twin = new Twin();
        twin.Properties.Desired = new TwinCollection();
        twin.Properties.Desired[keyPath] = "value";

        var twinJson = JObject.FromObject(twin.Properties.Desired);
        var keyElement = twinJson.SelectToken(keyPath);

        var dataToSign = Encoding.UTF8.GetBytes(keyElement.ToString());
        var expectedSignature = _ecdsaMock.Object.SignData(dataToSign, HashAlgorithmName.SHA512);
        var expectedSignatureString = Convert.ToBase64String(expectedSignature);

        InitKeyFromEnvirementVar();
        await _signingService.Init();

        var updateTwinCalled = false;
        _registryManagerMock.Setup(mock => mock.GetTwinAsync(deviceId))
                        .ReturnsAsync(twin)
                        .Verifiable();

        _registryManagerMock.Setup(mock => mock.UpdateTwinAsync(deviceId, It.IsAny<Twin>(), twin.ETag))
                        .Callback<string, Twin, string>((id, updatedTwin, eTag) =>
                        {
                            Assert.IsNotNull(updatedTwin.Properties.Desired);
                            Assert.IsTrue(updatedTwin.Properties.Desired.Contains(signatureKey));
                            Assert.AreEqual(expectedSignatureString, updatedTwin.Properties.Desired[signatureKey]);

                            updateTwinCalled = true;
                        })
                        .ReturnsAsync(twin)
                        .Verifiable();

        await _signingService.CreateTwinKeySignature(deviceId, keyPath, signatureKey);
        Assert.IsTrue(updateTwinCalled);
    }

    [Test]
    public async Task CreateTwinKeySignature_InvalidJsonPath_ThrowsArgumentException()
    {
        var deviceId = "testDevice";
        var invalidKeyPath = "invalidpath";
        var signatureKey = "signatureKey";

        var twin = new Twin();
        twin.Properties.Desired = new TwinCollection();
        twin.Properties.Desired["path"] = "value";

        _registryManagerMock.Setup(mock => mock.GetTwinAsync(deviceId))
                        .ReturnsAsync(twin)
                        .Verifiable();

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _signingService.CreateTwinKeySignature(deviceId, invalidKeyPath, signatureKey));

        Assert.AreEqual("Invalid JSON path specified", ex.Message);
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Invalid JSON path specified")), It.Is<Exception>(e => e == ex)), Times.Once);
        _registryManagerMock.Verify();
    }
}
