# Abstract
Here is the publish directory, where the publish.ps1 script creates self-contained deployments per OS and CPU architecture.

# Setup instructions
````
cd env/prod/
````
## Linux and MacOS
````
chmod +x startagent.sh
./startagent.sh <ARCHITECTURE_DIR>
````
For <ARCHITECTURE_DIR>, you can choose from the following options:

    linux-x64: Use this option for 64-bit Linux architecture.

## Windows
````
./startagent.bat <ARCHITECTURE_DIR>
````

For example
````
startagent.bat win-x64 
````

### working directory arg
startagent.bat can run from any local location with get the working dir in argument(path to env\dev or env\prod)
````
./startagent.bat <ARCHITECTURE_DIR> <WORKINGDIR>
````

For example:
````
startagent.bat win-x64 c:\env\dev
````
**Note: this option enable only when running from cmd (not with --winsrv option)**

### Windows - windows service
```
./startagent.bat <ARCHITECTURE_DIR> --winsrv <USER_PASSWORD>
```

The <USER_PASSWORD> is optional if `UserPassword` not set in appsettings.json and not want to use interactive input.

For example
````
startagent.bat win-x64 --winsrv
````

````
startagent.bat win-x64 --winsrv abcABC123!
````

### Windows service user permissions
#### **Admin** privileges are needed to **create** windows service, and generate certificate at **LocalMachine** store
#### The folowing permissions are needed to **use** windowes service:
* Read and Write permission to folders that are used in recipes for download & upload
* User should be logged on as service (`Local Security Policy -> Local Policy -> User Rights Assignment -> Log on as a service -> Add User or Group`)

If user detailes not configure in appsettings (`StoreLocation`, `Domain`, `UserName`, `UserPassword`) the service will be instaled with Admin rights, and certificate in LocalMachine.

## appsettings.json Configuration

To configure the application settings, please refer to the appsettings.json file and customize the following parameters as needed.
| Setting Name   | Description                    | Default Value   |
| ---------------| ------------------------------ | --------------- |
| `Authentication.GlobalDeviceEndpoint`       | the global device endpoint  |  `global.azure-devices-provisioning.net` |
| `Authentication.CertificateExpiredDays`       | Certificate expired days  |  `365` |
| `Authentication.DpsScopeId`       | DPS scope id  |  `true` |
| `Authentication.GroupEnrollmentKey`       | DPS enrollment group key  |   |
| `Authentication.StoreLocation`       | location to store the certificate - LocalMachine\CurrentUser.  |  if this value not configure - the default is LocalMachine |
| `Authentication.Domain`       | machine domain name  |  if this value not configure - the default is `.` |
| `Authentication.UserName`       | the user name log on as a service  |  if this value not configure - the default is Admin |
| `Authentication.UserPassword`       | the user password log on as a service  | if this value not configure - try to get the password from command line argument, if it is also not send it waiting to user interactive input.  |
| `StrictModeSettings.StrictMode`  | Strict mode flag  | `false`     |
| `StrictModeSettings.ProvisionalAuthenticationMethods`  | Method for provisional authentication  | `SAS`     |
| `StrictModeSettings.PermanentAuthenticationMethods`    | Method for permanent authentication | `X509`         |
| `StrictModeSettings.GlobalPatterns`    | general file access permissions or restrictions across the application | `[]` |
| `StrictModeSettings.FilesRestrictions`    | collection of restrictions or rules that apply specifically to file operations within the application | `{}` |
| `StrictModeSettings.FilesRestrictions.Id`    |  unique identifier for the specific restriction set | `LogUpload` |
| `StrictModeSettings.FilesRestrictions.Type`    |  specifies the type of action that the restrictions apply to | `Upload` or  `Download` |
| `StrictModeSettings.FilesRestrictions.Root`    |  represents the root directory or location where the specified file restrictions apply | `c:/` |
| `StrictModeSettings.FilesRestrictions.MaxSize`    |  maximum size limit for file downloads | `1` |
| `StrictModeSettings.FilesRestrictions.AllowPatterns`    |  array that contains patterns specifying the types of files that are allowed for the defined action | `[]`       |
| `StrictModeSettings.FilesRestrictions.DenyPatterns`    |  array that contains patterns specifying the types of files that are not allowed for the defined action | `[]`       |
| `DownloadSettings.SignFileBufferSize`    | sing documents buffer size | `16384`         |
| `DownloadSettings.CommunicationDelaySeconds`    | download delay seconds for check less communication | `30`         |
| `DownloadSettings.BlockedDelayMinutes`    | file blocked delay minutes | `10`         |
| `CommunicationLess`    | API returns mocks and not connect to IOT hub | `false`         |
| `RunDiagnosticsSettings.FileSizeBytes`    | The size of the file to be created for diagnostics  |131072         |
| `RunDiagnosticsSettings.PeriodicResponseWaitSeconds`    | Defines the time in seconds that the diagnostics process should check the download status | 10         |
| `RunDiagnosticsSettings.ResponseTimeoutMinutes`    | Sets the maximum time duration in minutes for which the diagnostics process should wait for a response before timing out | 5         |
| `UploadCompleteRetrySettings.MaxRetries`    | Specifies the maximum number of retry attempts that will be made in case of a failed upload or completion task | 3         |
| `UploadCompleteRetrySettings.DelaySeconds`    | Defines the delay duration in seconds between successive retry attempts | 30         |
| `AgentServiceName`    | Windows service agent name to install and start | `CARTO v8 CloudPillar`         |


## Call GetDeviceState in C# Example
```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public class DeviceStateClient
{
    private static readonly string baseUrl = "http://localhost:8099"; // Use HTTP
    private static readonly string deviceId = "your-device-id";
    private static readonly string secretKey = "your-secret-key";

    public async Task GetDeviceStateAsync()
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.AllowAutoRedirect = true;
        using (var client = new HttpClient(handler))
        {
            client.BaseAddress = new Uri(baseUrl);

            // Add the required headers
            client.DefaultRequestHeaders.Add("X-device-id", deviceId);
            client.DefaultRequestHeaders.Add("X-secret-key", secretKey);

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
```

## Twin json Description & Example

```json
{
  "changeSpec": {
    "id": "1.17.66419.44823.20230425145510",
    "changeSign": "AauRfo25DQRQGwtWSkr89gJQOX7YhL17vKI2dSyhXktKYBR33nhHOB4mDLPeRvf33uwWYt9+xzRJ/Xd8GHvdk5AwAIj6fp6gnYuMs+XQnlvis/jedyQEoqF+owrl5RrW2bfdzRs",
    "patch": {
      "transitPackage": [
            {
                "action": "PeriodicUpload",
                "description": "Periodically (once in 10min) upload installation logging",
                "filename": "I:\\ExportedData_2023.05.*",
                "interval": 120,
                "enabled": true
            },
            {
                "action": "SingularDownload",
                "description": "Carto 7.2 SPU Patch",
                "source": "SPU.zip",
                "protocol": "https|iotamqp|iotmqtt",
                "sign": "AauRfo25DQRQGwtWSkr89gJQOX7YhL17vKI2dSyhXktKYBR33nhHOB4mDLPeRvf33uwWYt9+xzRJ/Xd8GHvdk5AwAIj6fp6gnYuMs+XQnlvis/jedyQEoqF+owrl5RrW2bfdzRsNgrusKUQpxQ3jCWS6gO9aQYTa5QhSNzjMFgckIdnx",
                "destinationPath": "./SPU.zip"
            }
        ],
       "preInstallConfig": [
            {
                "action": "ExecuteOnce",
                "description": "Extraction of security update McAfee",
                "shell": "powershell",
                "command": "Expand-Archive -LiteralPath '.\\mcaffeeV3_5150dat.zip' -DestinationPath 'I:\\' -Force"
            },
        ]
    }
  }
}
```
## Twin json description

- `changeSpec`: The main object representing updates.
  - `id`: A unique identifier for the updates.
  - `changeSign`: The twin desired signature.
  - `patch`: The "patch" section of the JSON structure contains details specific to the process. It includes the following five lists of actions to be executed in a specific order:
    1. `preTransitConfig`: Configuration settings to be applied before the patch transit. These actions are performed prior to downloading or installing the firmware patch.

    2. `transitPackage`: Actions related to the transit of the firmware package. This includes downloading the firmware artifact from a source, specifying communication protocols, and defining the destination path.

    3. `preInstallConfig`: Configuration settings to be applied before the actual installation of the firmware. This can involve tasks like extracting the downloaded firmware.

    4. `installSteps`: A sequence of actions that detail the steps to install the firmware. This may include running scripts or commands to perform the installation.

    5. `postInstallConfig`: Configuration settings and actions to be executed after the firmware installation. These actions can include tasks such as triggering system updates or verifying the integrity of the installed firmware.
    
    **Actions Description**

    The firmware update process involves various actions that are organized into different arrays. Each action corresponds to a specific task or operation within the update process. Below, we describe the common properties associated with these actions and their roles in the update workflow. Please refer to the individual sections for detailed descriptions of each action:

    - `action`: Describes the type of operation to be performed (e.g., SingularDownload, SingularUpload, PeriodicUpload, ExecuteOnce).
    - `description`: Provides a brief explanation of the action's purpose or objective.
    - Additional properties specific to each action may be presesented, depending on the array.

    
    Details of each action:
    1. **Periodic Upload Action**:
        - `action`: PeriodicUpload
        - `description`: Periodically (once in 10 minutes) upload installation logging.
        - `dirName`: The directory to upload (e.g., "I:\\ExportedData_2023.05\\"). OR
        - `fileName`: The file to upload (e.g., "I:\\ExportedData_2023.05\\aaa.txt").
        - `interval`: The time interval between uploads in seconds (e.g., 120 seconds).

    2. **Singular Upload Action**:
        - `action`: SingularUpload
        - `description`: upload data.
        - `fileName`: The file or pattern to be uploaded (e.g., "I:\\ExportedData_2023.05.*").
        - `method`: Method for upload, Blob or Stream.

    3. **Singular Download Action**:
        - `action`: SingularDownload
        - `description`: Download Carto 7.2 SPU Patch.
        - `source`: The source of the firmware package (e.g., "SPU.zip").
        - `sign`: Signature of file content.
        - `destinationPath`: The destination path for storing the downloaded firmware (e.g., "./SPU.zip").
        - `unzip`: Do unzip to download (The file must be a ZIP).
    
    4. **Execute Once Action**:
        - `action`: ExecuteOnce
        - `description`: Extraction of security update McAfee.
        - `shell`: The shell or scripting language used to execute the command (e.g., "powershell").
        - `command`: The command to extract an archive (e.g., "Expand-Archive -LiteralPath '.\\mcaffeeV3_5150dat.zip' -DestinationPath 'I:\\' -Force").
        - `onPause`: This property specifies the command to be executed when the update process is paused..
        - `onResume`: When the firmware update process resumes.



