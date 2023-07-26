using NUnit.Framework;
using Moq;
using System.Net;
using System.Text;
using Shared.Logger;
using common;

namespace common.tests;
public class HttpRequestorTestFixture
{
    private IHttpRequestorService _httpRequestor;
    private Mock<ISchemaValidator> _validatorMock;
    private Mock<HttpClient> _httpClientMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private string _testUrl = "https://example.com";

    [SetUp]
    public void SetUp()
    {
        _httpClientMock = new Mock<HttpClient>();
        _httpClientMock.SetupAllProperties();

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _httpClientMock
            .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClientMock.Object);

        _validatorMock = new Mock<ISchemaValidator>();
        _validatorMock.Setup(v => v.ValidatePayloadSchema(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

        _loggerHandlerMock = new Mock<ILoggerHandler>();

        _httpRequestor = new HttpRequestorService(httpClientFactoryMock.Object, _validatorMock.Object, _loggerHandlerMock.Object);
    }

    #region TestSendRequest
    [Test]
    public async Task SendRequest_ValidUrl_ReturnsResponse()
    {
        var requestData = new { Name = "try" };
        CancellationToken cancellationToken = default;
        async Task SendRequest() => await _httpRequestor.SendRequest(_testUrl, HttpMethod.Post, requestData, cancellationToken);
        Assert.DoesNotThrowAsync(SendRequest);
    }

    [Test]
    public async Task SendRequest_InvalidUrl_ThrowsException()
    {
        string url = "invalid url";
        async Task SendRequest() => await _httpRequestor.SendRequest(url, HttpMethod.Get);
        Assert.ThrowsAsync<System.InvalidOperationException>(SendRequest);
        _loggerHandlerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Invalid Url")), 
           It.IsAny<System.InvalidOperationException>()), Times.Once);
    }

    [Test]
    public async Task SendRequest_ThrowsException()
    {
        _httpClientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new HttpRequestException("Failed to send request.", null, HttpStatusCode.InternalServerError));
        try
        {
            async Task SendRequest() => await _httpRequestor.SendRequest<string>(_testUrl, HttpMethod.Get);
            Assert.ThrowsAsync<HttpRequestException>(SendRequest);
        }
        catch(HttpRequestException)
        {
            _loggerHandlerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains($"HTTP request failed with status code")),
                It.IsAny<HttpRequestException>()), Times.Once);
        }
    }

    #endregion

    #region TestRequestTimeOut
    [Test]
    public void SetTimeoutForHttpsRequest_WithValidEnvironmentVariable_SetsTimeout()
    {
        string httpsTimeoutSecondsString = "60"; // Valid environment variable value
        Environment.SetEnvironmentVariable(CommonConstants.httpsTimeoutSeconds, httpsTimeoutSecondsString);

        _httpRequestor.SendRequest<object>(_testUrl, HttpMethod.Get);
        Assert.That(_httpClientMock.Object.Timeout, Is.EqualTo(TimeSpan.FromSeconds(60)));
    }

    [Test]
    public void SetTimeoutForHttpsRequest_WithInvalidEnvironmentVariable_UsesDefaultTimeout()
    {
        string httpsTimeoutSecondsString = "invalid-value"; // Invalid environment variable value
        Environment.SetEnvironmentVariable(CommonConstants.httpsTimeoutSeconds, httpsTimeoutSecondsString);

        _httpRequestor.SendRequest<object>(_testUrl, HttpMethod.Get);
        Assert.That(_httpClientMock.Object.Timeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public void SetTimeoutForHttpRequest_UsesDefaultTimeout()
    {
        _httpRequestor.SendRequest<object>(_testUrl, HttpMethod.Get);
        Assert.That(_httpClientMock.Object.Timeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    #endregion


    #region TestRequestSchemaValidation
    [Test]
    public async Task SendRequest_InvalidSchema_ThrowsHttpRequestException()
    {
        var requestData = new { Name = "John" };
        _validatorMock.Setup(v => v.ValidatePayloadSchema(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false); // Simulate an invalid schema

        async Task SendRequest() => await _httpRequestor.SendRequest(_testUrl, HttpMethod.Post, requestData);
        Assert.ThrowsAsync<HttpRequestException>(SendRequest);
        _loggerHandlerMock.Verify(l => l.Error(
            It.Is<string>(msg => msg.Contains("The request data is not fit the schema")),
            It.IsAny<System.Net.Http.HttpRequestException>()), Times.Once);
    }

    [Test]
    public void SendRequest_InvalidResponseSchema_ThrowsException()
    {
        var invalidResponseContent = "{ \"id\": 1 }";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(invalidResponseContent, Encoding.UTF8, "application/json")
        };
        _httpClientMock.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        _validatorMock.Setup(v => v.ValidatePayloadSchema(invalidResponseContent, It.IsAny<string>(), false))
            .Returns(false);
        Assert.ThrowsAsync<HttpRequestException>(() => _httpRequestor.SendRequest<object>(_testUrl, HttpMethod.Get));
        _loggerHandlerMock.Verify(l => l.Error(
            It.Is<string>(msg => msg.Contains("The reponse data is not fit the schema")),
            It.IsAny<System.Net.Http.HttpRequestException>()), Times.Once);
    }

    #endregion

}
