namespace common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

public class SchemaValidator
{
    private readonly ConcurrentDictionary<string, JSchema> SchemaCache = new ConcurrentDictionary<string, JSchema>();

 

    public JToken ParseAndValidate(string payload, string schemaPath)
    {
        JSchema jSchema = SchemaCache.GetOrAdd(schemaPath, LoadAndParseSchema);

 

        JToken jToken = JToken.Parse(payload);
        jToken.Validate(jSchema);

 

        return jToken;
    }

 

    private JSchema LoadAndParseSchema(string schemaPath)
    {
        string schema = File.ReadAllText(schemaPath);
        return JSchema.Parse(schema);
    }
}