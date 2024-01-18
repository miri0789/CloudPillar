using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Sevices.Interfaces;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class AuthorizationCheckMiddlewareTests
{
    private AuthorizationCheckMiddleware _target;
    private ControllerContext _controllerContext;
    private Mock<RequestDelegate> _requestDelegate;
    private Mock<ILoggerHandler> _logger;
    private Mock<IConfigurationWrapper> _configuration;
    private Mock<IDPSProvisioningDeviceClientHandler> _dPSProvisioningDeviceClientHandler;
    private Mock<IProvisioningService> _provisioningService;
    private Mock<ISymmetricKeyProvisioningHandler> _symmetricKeyProvisioningHandler;
    private Mock<IStateMachineHandler> _stateMachineHandler;
    private Mock<IX509Provider> _x509Provider;
    private Mock<IHttpContextWrapper> _httpContextWrapper;

    [SetUp]
    public void Setup()
    {
        _controllerContext = new ControllerContext();
        _requestDelegate = new Mock<RequestDelegate>();
        _logger = new Mock<ILoggerHandler>();
        _configuration = new Mock<IConfigurationWrapper>();
        _dPSProvisioningDeviceClientHandler = new Mock<IDPSProvisioningDeviceClientHandler>();
        _provisioningService = new Mock<IProvisioningService>();
        _symmetricKeyProvisioningHandler = new Mock<ISymmetricKeyProvisioningHandler>();
        _stateMachineHandler = new Mock<IStateMachineHandler>();
        _x509Provider = new Mock<IX509Provider>();
        _httpContextWrapper = new Mock<IHttpContextWrapper>();

        var services = new ServiceCollection();
        services.AddSingleton<IProvisioningService>(_provisioningService.Object);
        services.AddSingleton<ISymmetricKeyProvisioningHandler>(_symmetricKeyProvisioningHandler.Object);
        var serviceProvider = services.BuildServiceProvider();
        _controllerContext.HttpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        _target = new AuthorizationCheckMiddleware(_requestDelegate.Object, _logger.Object, _configuration.Object);
    }

    [Test]
    public async Task Invoke_OnNonHttpsRequest_RedirectsToHttps()
    {
        _controllerContext.HttpContext.Request.IsHttps = false;
        _configuration.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<int>())).Returns(8080);
        _httpContextWrapper.Setup(x => x.GetDisplayUrl(It.IsAny<HttpContext>())).Returns("http://example.com/path");
        await _target.Invoke(_controllerContext.HttpContext, _dPSProvisioningDeviceClientHandler.Object, _stateMachineHandler.Object, _x509Provider.Object, _httpContextWrapper.Object);
        _x509Provider.Verify(x => x.GetHttpsCertificate(), Times.Once);
    }

    [Test]
    public async Task Invoke_OnDeviceIdOrSecretIdEmpty_ReturnError()
    {
        _controllerContext.HttpContext.Request.IsHttps = true;
        _httpContextWrapper.Setup(x => x.GetEndpoint(It.IsAny<HttpContext>())).Returns(
            new Endpoint((context) => Task.CompletedTask, new EndpointMetadataCollection(new ControllerActionDescriptor()), "MyEndpointName"));
        await _target.Invoke(_controllerContext.HttpContext, _dPSProvisioningDeviceClientHandler.Object, _stateMachineHandler.Object, _x509Provider.Object, _httpContextWrapper.Object);
        _symmetricKeyProvisioningHandler.Verify(x => x.AuthorizationDeviceAsync(new CancellationToken()), Times.Never);
        _logger.Verify(x => x.Error(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task Invoke_OnNotIsX509Authorized_ProvisinigAndReturnError()
    {
        _controllerContext.HttpContext.Request.IsHttps = true;
        _httpContextWrapper.Setup(x => x.TryGetValue(
                It.IsAny<IDictionary<string, Microsoft.Extensions.Primitives.StringValues>>(), It.IsAny<string>(), out It.Ref<Microsoft.Extensions.Primitives.StringValues>.IsAny))
            .Callback((IDictionary<string, Microsoft.Extensions.Primitives.StringValues> dictionary, string key, out Microsoft.Extensions.Primitives.StringValues value) =>
            {
                value = new Microsoft.Extensions.Primitives.StringValues("mockedValue");
            }).Returns(true);

        _httpContextWrapper.Setup(x => x.GetEndpoint(It.IsAny<HttpContext>())).Returns(
                new Endpoint((context) => Task.CompletedTask, new EndpointMetadataCollection(new ControllerActionDescriptor()), "MyEndpointName"));

        await _target.Invoke(_controllerContext.HttpContext, _dPSProvisioningDeviceClientHandler.Object, _stateMachineHandler.Object, _x509Provider.Object, _httpContextWrapper.Object);
        _symmetricKeyProvisioningHandler.Verify(x => x.AuthorizationDeviceAsync(new CancellationToken()), Times.Once);
        _logger.Verify(x => x.Error(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task Invoke_OnGetDeviceState_ProvisinigNoError()
    {
        _controllerContext.HttpContext.Request.IsHttps = true;
        _controllerContext.HttpContext.Request.Path = "/api/v1/getdevicestate";
        _httpContextWrapper.Setup(x => x.TryGetValue(
                It.IsAny<IDictionary<string, Microsoft.Extensions.Primitives.StringValues>>(), It.IsAny<string>(), out It.Ref<Microsoft.Extensions.Primitives.StringValues>.IsAny))
            .Callback((IDictionary<string, Microsoft.Extensions.Primitives.StringValues> dictionary, string key, out Microsoft.Extensions.Primitives.StringValues value) =>
            {
                value = new Microsoft.Extensions.Primitives.StringValues("mockedValue");
            }).Returns(true);

        _httpContextWrapper.Setup(x => x.GetEndpoint(It.IsAny<HttpContext>())).Returns(
                new Endpoint((context) => Task.CompletedTask, new EndpointMetadataCollection(new ControllerActionDescriptor()), "MyEndpointName"));

        await _target.Invoke(_controllerContext.HttpContext, _dPSProvisioningDeviceClientHandler.Object, _stateMachineHandler.Object, _x509Provider.Object, _httpContextWrapper.Object);
        _symmetricKeyProvisioningHandler.Verify(x => x.AuthorizationDeviceAsync(new CancellationToken()), Times.Once);
        _logger.Verify(x => x.Error(It.IsAny<string>()), Times.Never);
    }
}