namespace common;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;
using System.IO;

public interface ISchemaValidator
{
    bool ValidatePayloadSchema(string payload, string schemaPath);
} 

public class SchemaValidator: ISchemaValidator
{
    private readonly ConcurrentDictionary<string, JSchema> SchemaCache = new ConcurrentDictionary<string, JSchema>();

    public bool ValidatePayloadSchema(string payload, string schemaPath)
    {
        JSchema jSchema = SchemaCache.GetOrAdd(schemaPath, LoadSchema);
        JToken jToken = JToken.Parse(payload);
        bool isValid = jToken.IsValid(jSchema);

        return isValid;
    }


    private JSchema LoadSchema(string schemaPath)
    {
        string schema = File.ReadAllText(schemaPath);
        return JSchema.Parse(schema);
    }
}