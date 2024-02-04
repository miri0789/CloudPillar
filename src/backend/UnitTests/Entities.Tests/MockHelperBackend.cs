using Newtonsoft.Json;

public static class MockHelperEntities
{
    public static bool EqualObjects(object expectedResult, object result)
    {
        return JsonConvert.SerializeObject(expectedResult) == JsonConvert.SerializeObject(result);
    }
}