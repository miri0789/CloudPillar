using System.Security.Cryptography;
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

namespace Backend.Keyholder.Tests;


public class SigningTestFixture
{
    private ISigningService _target;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLogger;
    private Mock<IRegistryManagerWrapper> _registryManagerWrapper;
    private Mock<ECDsa> _ecdsaMock;

    [SetUp]
    public void Setup()
    {
        _ecdsaMock = new Mock<ECDsa>();
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLogger = new Mock<ILoggerHandler>();
        _registryManagerWrapper = new Mock<IRegistryManagerWrapper>();
        _mockEnvironmentsWrapper.Setup(c => c.iothubConnectionString).Returns("HostName=szlabs-iot-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dMBNypodzUSWPbxTXdWaV4PxJTR3jCwehPFCQn+XJXc=");
        _mockEnvironmentsWrapper.Setup(c => c.signingPem).Returns("");
        _mockEnvironmentsWrapper.Setup(c => c.kubernetesServiceHost).Returns("your-kubernetes-service-host");
        _mockEnvironmentsWrapper.Setup(c => c.secretName).Returns("");
        _mockEnvironmentsWrapper.Setup(c => c.secretKey).Returns("your-secret-key");
        var registryManagerMock = new Mock<RegistryManager>();
        _registryManagerWrapper.Setup(mock => mock.CreateFromConnectionString())
                        .Returns(registryManagerMock.Object);
        _target = new SigningService(_mockEnvironmentsWrapper.Object, _mockLogger.Object, _registryManagerWrapper.Object);
    }


    private void InitKeyFromEnvirementVar()
    {
        string pemContent = @"LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSHVBZ0VBTUJBR0J5cUdTTTQ5QWdFR0JTdUJCQUFqQklIV01JSFRBZ0VCQkVJQml5QWE3YVJIRkRDaDJxZ2EKOXNUVUdJTkU1akhBRm5tTTh4V2VUL3VuaTVJNHROcWhWNVh4MHBEcm1DVjltYnJvRnRmRWEwWFZmS3VNQXh4ZgpaNkxNL3lLaGdZa0RnWVlBQkFHQnpnZG5QNzk4RnNMdVdZVEREUUE3YzByM0JWazhOblJVU2V4cFFVc1JpbFBOCnYzU2NoTzBsUnc5UnU4Nngxa2huVkR4K2R1cTRCaURGY3ZsU0FjeWpMQUNKdmp2b3lUTEppQStUUUZkbXJlYXIKak1pWk5FMjVwVDJ5V1AxTlVuZEp4UGN2VnRmQlc0OGtQT212a1k0V2xxUDViQXdDWHdic0tyQ2drNnhic3AxMgpldz09Ci0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0=";
        _mockEnvironmentsWrapper.Setup(c => c.signingPem).Returns(pemContent);
    }

    [Test]
    public async Task CreateTwinKeySignature_ValidArguments_SignatureAddedToTwin()
    {
        var deviceId = "testDevice";
        var keyPath = "path";
        var signatureKey = "changeSign";

        var twin = new Twin();
        twin.Properties.Desired = new TwinCollection();
        twin.Properties.Desired[keyPath] = "value";

        var twinJson = JObject.FromObject(twin.Properties.Desired);
        var keyElement = twinJson.SelectToken(keyPath)!;

        var dataToSign = Encoding.UTF8.GetBytes(keyElement.ToString());
        var expectedSignature = _ecdsaMock.Object.SignData(dataToSign, HashAlgorithmName.SHA512);
        var expectedSignatureString = Convert.ToBase64String(expectedSignature);

        InitKeyFromEnvirementVar();

        _registryManagerWrapper.Setup(mock => mock.UpdateTwinAsync(It.IsAny<RegistryManager>(), deviceId, It.IsAny<Twin>(), twin.ETag))
                        .ReturnsAsync(twin)
                        .Verifiable();

        await _target.CreateTwinKeySignature(deviceId);
    }
}
