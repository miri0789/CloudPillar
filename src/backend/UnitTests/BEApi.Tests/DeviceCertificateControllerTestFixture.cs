using Backend.BEApi.Services.Interfaces;
using Moq;
using Shared.Logger;
using Backend.BEApi.Controllers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Backend.BEApi.Tests;

public class DeviceCertificateControllerTestFixture
{
    private DeviceCertificateController _target;
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IDeviceCertificateService> _certificateServiceMock;
    private Mock<ICertificateIdentityService> _certificateIdentityServiceMock;


    [SetUp]
    public void Setup()
    {
        _certificateServiceMock = new Mock<IDeviceCertificateService>();
        _certificateIdentityServiceMock = new Mock<ICertificateIdentityService>();
        _loggerMock = new Mock<ILoggerHandler>();


        _target = new DeviceCertificateController(_certificateServiceMock.Object, _certificateIdentityServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ProcessNewSigningCertificate_ValidProcess_Success()
    {
        _target.ProcessNewSigningCertificate("deviceId");
        _certificateIdentityServiceMock.Verify(h => h.ProcessNewSigningCertificate("deviceId"), Times.Once);
    }

    [Test]
    public async Task ProcessNewSigningCertificate_ValidData_Success()
    {
        _certificateIdentityServiceMock.Setup(h => h.ProcessNewSigningCertificate("deviceId")).Throws(new Exception("Error"));
        var res = await _target.ProcessNewSigningCertificate("deviceId");
        Assert.AreEqual((res as ObjectResult).StatusCode, 400);
    }

}
