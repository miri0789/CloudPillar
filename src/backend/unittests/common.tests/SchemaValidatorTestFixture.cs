using NUnit.Framework;
using Moq;
using Shared.Logger;
using common;

[TestFixture]
public class SchemaValidatorTestFixture
{
    private SchemaValidator _validator;
    private Mock<ILoggerHandler> _loggerHandlerMock;

    [SetUp]
    public void SetUp()
    {
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _validator = new SchemaValidator(_loggerHandlerMock.Object);
    }

    [Test]
    public void ValidatePayloadSchema_ValidPayload_ReturnsTrue()
    {
        string payload = "{\"name\": \"John\", \"age\": 30}";
        string schemaPath = "tests/test";
        bool isRequest = true;

        bool isValid = _validator.ValidatePayloadSchema(payload, schemaPath, isRequest);

        Assert.IsTrue(isValid);
    }

    [Test]
    public void ValidatePayloadSchema_InvalidPayload_ReturnsFalse()
    {
        string payload = "{\"name\": \"John\", \"age\": \"thirty\"}";
        string schemaPath = "tests/test";
        bool isRequest = true;

        bool isValid = _validator.ValidatePayloadSchema(payload, schemaPath, isRequest);

        Assert.IsFalse(isValid);

        _loggerHandlerMock.Verify(l => l.Error(It.IsAny<string>()), Times.Once);
    }

    
}
