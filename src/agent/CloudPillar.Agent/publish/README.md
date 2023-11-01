# Abstract
Here is the publish directory, where the publish.ps1 script creates self-contained deployments per OS and CPU architecture.

# Setup instructions
## Linux and MacOS
````
chmod +x startagent.sh
./startagent.sh <ARCHITECTURE_DIR>
````

## Windows
````
./startagent.bat <ARCHITECTURE_DIR>
````

For example
````
startagent.bat win-x64 
````

## appsettings.json Configuration
````
To configure the application settings, please refer to the appsettings.json file and customize the following parameters as needed.

| Setting Name   | Description                    | Default Value   |
| --------------- | ------------------------------ | --------------- |
| `GlobalDeviceEndpoint`       | the global device endpoint  |  `global.azure-devices-provisioning.net` |
| `CertificateExpiredDays`       | Certificate expired days  |  `365` |
| `DpsScopeId`       | DPS scope id  |  `true` |
| `GroupEnrollmentKey`       | DPS enrollment group key  |   |
| `ProvisionalAuthentucationMethods`  | Method for provisional authentucation  | `SAS`     |
| `PermanentAuthentucationMethods`    | Method for permanent authentucation | `X509`         |
| `GlobalPatterns`    | general file access permissions or restrictions across the application | `[]` |
| `FilesRestrictions`    | collection of restrictions or rules that apply specifically to file operations within the application | `{}` |
| `Id`    |  unique identifier for the specific restriction set | `LogUpload` |
| `Type`    |  specifies the type of action that the restrictions apply to | `Upload` or  `Download` |
| `Root`    |  represents the root directory or location where the specified file restrictions apply | `c:/` |
| `MaxSize`    |  maximum size limit for file downloads | `1` |
| `AllowPatterns`    |  array that contains patterns specifying the types of files that are allowed for the defined action | `[]`       |
| `DenyPatterns`    |  array that contains patterns specifying the types of files that are not allowed for the defined action | `[]`       |
````

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public class DeviceStateClient
{
    private static readonly string baseUrl = "http://localhost:8099";
    private static readonly string deviceId = "your-device-id";
    private static readonly string secretKey = "your-secret-key";

    public async Task GetDeviceStateAsync()
    {
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(baseUrl);

            // Add the required headers
            client.DefaultRequestHeaders.Add("X-device-id", deviceId);
            client.DefaultRequestHeaders.Add("X-secret-key", secretKey);

            // Make the GET request to GetDeviceState
            HttpResponseMessage response = await client.GetAsync("Agent/GetDeviceState");

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Device State: " + result);
            }
            else
            {
                Console.WriteLine("Failed to retrieve device state. Status code: " + response.StatusCode);
            }
        }
    }
}
