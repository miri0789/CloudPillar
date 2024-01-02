
using System.Text;
using CloudPillar.Agent.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Utilities;
public class CommunicationLessMiddleware
{
    private readonly RequestDelegate _next;
    private static string _twinJson = @"
{
    ""deviceId"": ""try1"",
    ""etag"": null,
    ""version"": null,
    ""properties"": {
        ""desired"": {
            ""changeSpec"": {
                ""id"": ""1.17.66419.44823.20230425145510"",
                ""patch"": {
                    ""transitPackage"": [
                        {
                            ""actionId"": ""1234"",
                            ""action"": ""SingularDownload"",
                            ""description"": ""Carto 7.2 SPU Patch 1"",
                            ""source"": ""1111.txt"",
                            ""protocol"": ""https|iotamqp|iotmqtt"",
                            ""destinationPath"": ""C:\\Users\\mmarzbac\\Downloads\\S31772750\\""
                        },
                        {
                            ""actionId"": ""5678"",
                            ""action"": ""SingularDownload"",
                            ""description"": ""Carto 7.2 SPU Patch 3"",
                            ""source"": ""2222.txt"",
                            ""protocol"": ""https|iotamqp|iotmqtt"",
                            ""destinationPath"": ""C:\\Users\\mmarzbac\\Downloads\\S317727502\\""
                        },
                        {
                            ""actionId"": ""97899"",
                            ""action"": ""SingularDownload"",
                            ""description"": ""Carto 7.2 SPU Patch 3"",
                            ""source"": ""3333.txt"",
                            ""protocol"": ""https|iotamqp|iotmqtt"",
                            ""destinationPath"": ""C:\\Users\\mmarzbac\\Downloads\\S317727503\\""
                        },
                        {
                            ""actionId"": ""111111"",
                            ""action"": ""SingularUpload"",
                            ""description"": ""Periodically (once in 10min) upload installation logging"",
                            ""filename"": ""I:\\ExportedData_2023.05.*""
                        },
                        {
                            ""actionId"": ""111111"",
                            ""action"": ""SingularUpload"",
                            ""description"": ""Periodically (once in 10min) upload installation logging"",
                            ""filename"": ""I:\\ExportedData_2023.05.*""
                        }
                    ]
                }
            },
            ""$version"": 12
        },
        ""reported"": {
            ""deviceState"": ""Ready"",
            ""secretKey"": ""1111"",
            ""changeSpec"": {
                ""patch"": {
                    ""transitPackage"": [
                        {
                            ""status"": ""Success""
                        },
                        {
                            ""status"": ""Failed"",
                            ""ResultText"": ""Failed reason""
                        },
                        {
                            ""status"": ""InProgress"",
                            ""progress"": 2.03
                        },
                        {
                            ""status"": ""Success""
                        },
                        {
                            ""status"": ""Failed"",
                            ""ResultText"": ""Failed reason""
                        },
                    ]
                },
                ""id"": ""1.17.66419.44823.20230425145510""
            },
            ""$version"": 36529
        }
    }
}";

    private static dynamic _twinJsonObject = JsonConvert.DeserializeObject(_twinJson)!;

    public CommunicationLessMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task Invoke(HttpContext context)
    {
        var functionName = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ActionName;
        _twinJsonObject.properties.reported.deviceState = DeviceStateType.Ready.ToString();
        _twinJsonObject.properties.deviceId = context.Request.Headers.TryGetValue(Constants.X_DEVICE_ID, out var deviceId) ? deviceId.ToString() : string.Empty;
        _twinJsonObject.properties.reported.secretKey = context.Request.Headers.TryGetValue(Constants.X_SECRET_KEY, out var secretKey) ? secretKey.ToString() : string.Empty;
        switch (functionName)
        {
            case "InitiateProvisioning": _twinJsonObject.properties.reported.deviceState = DeviceStateType.Provisioning.ToString(); break;
            case "SetBusy": _twinJsonObject.properties.reported.deviceState = DeviceStateType.Busy.ToString(); break;
            case "UpdateReportedProps":
                string requestBody;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    requestBody = await reader.ReadToEndAsync();
                }
                var updateReportedProps = JsonConvert.DeserializeObject<UpdateReportedProps>(requestBody)!;
                foreach (var item in updateReportedProps.Properties)
                {
                    _twinJsonObject.properties.reported[item.Name] = JToken.FromObject(item.Value);
                }
                break;
        }
        string jsonResponse = JsonConvert.SerializeObject(_twinJsonObject);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse, Encoding.UTF8);
    }

}
