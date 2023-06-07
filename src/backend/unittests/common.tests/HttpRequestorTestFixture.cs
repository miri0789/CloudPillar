using NUnit.Framework;
using Moq;
using System.Net;
using common;

namespace common.tests;
public class HttpRequestorTestFixture
{
    private HttpRequestorService httpRequestor;

    [SetUp]
    public void SetUp()
    {


        var httpClientMock = new Mock<HttpClient>();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpClientMock
            .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);

        var schemaValidatorMock = new Mock<ISchemaValidator>();
        schemaValidatorMock.Setup(v => v.ValidatePayloadSchema(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

        httpRequestor = new HttpRequestorService(httpClientFactoryMock.Object, schemaValidatorMock.Object);
    }


    [Test]
    public async Task SendRequest_ValidUrl_ReturnsResponse()
    {
        string url = "https://example.com";
        HttpMethod method = HttpMethod.Post;
        var requestData = new { Name = "try" };
        CancellationToken cancellationToken = default;
        await httpRequestor.SendRequest(url, method, cancellationToken);
    }
}
