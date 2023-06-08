using NUnit.Framework;
using common;

[TestFixture]
public class SchemaValidatorTestFixture
{
    private SchemaValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new SchemaValidator();
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
    }

    
}
