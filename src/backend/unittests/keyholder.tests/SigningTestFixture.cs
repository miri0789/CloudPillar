﻿using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices;
using Moq;
using k8s;
using k8s.Models;
using Microsoft.Azure.Devices.Shared;

namespace keyholder.tests;


public class SigningTestFixture
{
    private Mock<RegistryManager> _registryManagerMock;
    private Mock<ECDsa> _ecdsaMock;
    private SigningService _signingService;
    private Mock<Kubernetes> _kubernetesMock;

    [SetUp]
    public void Setup()
    {
        _registryManagerMock = new Mock<RegistryManager>();
        _ecdsaMock = new Mock<ECDsa>();
        Environment.SetEnvironmentVariable(Constants.iothubConnectionString, "HostName=szlabs-iot-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dMBNypodzUSWPbxTXdWaV4PxJTR3jCwehPFCQn+XJXc=");
        _signingService = new SigningService();
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
        Environment.SetEnvironmentVariable(Constants.signingPem, "");
        Environment.SetEnvironmentVariable(Constants.kubernetesServiceHost, "your-kubernetes-service-host");
        Environment.SetEnvironmentVariable(Constants.secretName, null);
        Environment.SetEnvironmentVariable(Constants.secretKey, "your-secret-key");

        async Task InitSigning() => await _signingService.Init();
        Assert.ThrowsAsync<InvalidOperationException>(InitSigning);
    }

    private void InitKeyFromEnvirementVar()
    {
        Environment.SetEnvironmentVariable(Constants.kubernetesServiceHost, "your-kubernetes-service-host");
        string pemContent = @"LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSHVBZ0VBTUJBR0J5cUdTTTQ5QWdFR0JTdUJCQUFqQklIV01JSFRBZ0VCQkVJQml5QWE3YVJIRkRDaDJxZ2EKOXNUVUdJTkU1akhBRm5tTTh4V2VUL3VuaTVJNHROcWhWNVh4MHBEcm1DVjltYnJvRnRmRWEwWFZmS3VNQXh4ZgpaNkxNL3lLaGdZa0RnWVlBQkFHQnpnZG5QNzk4RnNMdVdZVEREUUE3YzByM0JWazhOblJVU2V4cFFVc1JpbFBOCnYzU2NoTzBsUnc5UnU4Nngxa2huVkR4K2R1cTRCaURGY3ZsU0FjeWpMQUNKdmp2b3lUTEppQStUUUZkbXJlYXIKak1pWk5FMjVwVDJ5V1AxTlVuZEp4UGN2VnRmQlc0OGtQT212a1k0V2xxUDViQXdDWHdic0tyQ2drNnhic3AxMgpldz09Ci0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0=";
        Environment.SetEnvironmentVariable(Constants.signingPem, pemContent);

    }

    [Test]
    public async Task LoadPrivateKeyFromEnvirementVariable_ValidPrivateKey_InitECDsaInstance()
    {
        InitKeyFromEnvirementVar();
        await _signingService.Init();
        Assert.IsNotNull(_ecdsaMock.Object);
        Assert.IsInstanceOf<ECDsa>(_ecdsaMock.Object);
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
        _registryManagerMock.Verify();
    }
}
