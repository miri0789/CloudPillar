using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;
using System.IO;
using Shared.Logger;

namespace Backend.Infra.Common;
public interface ISchemaValidator
{
    bool ValidatePayloadSchema(string payload, string schemaPath, bool isRequest);
}

public class SchemaValidator : ISchemaValidator
{
    private readonly ConcurrentDictionary<string, JSchema> SchemaCache = new ConcurrentDictionary<string, JSchema>();
    private readonly string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema");
    private readonly ILoggerHandler _logger;
    public SchemaValidator(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public bool ValidatePayloadSchema(string payload, string schemaPath, bool isRequest)
    {
        var path = Path.Combine(basePath, GetUriDirectionPath(isRequest), $"{schemaPath}.json");
        JSchema jSchema = SchemaCache.GetOrAdd(path, LoadSchema, isRequest);
        JToken jToken = JToken.Parse(payload);
        bool isValid = jToken.IsValid(jSchema, out IList<string> errorMessages);
        if (!isValid)
        {
            foreach (var errorMessage in errorMessages)
            {
                _logger.Error(errorMessage);
            }
        }

        return isValid;
    }


    private JSchema LoadSchema(string schemaPath, bool isRequest = true)
    {
        if (File.Exists(schemaPath))
        {
            string schema = File.ReadAllText(schemaPath);
            return JSchema.Parse(schema);
        }
        else
        {
            var defaultPath = Path.Combine(basePath, GetUriDirectionPath(isRequest), "default.json");
            return LoadSchema(defaultPath);
        }
    }

    private string GetUriDirectionPath(bool isRequest)
    {
        return isRequest ? "request" : "response";
    }
}