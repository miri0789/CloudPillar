# Abstract
Here is the publish directory, where the publish.ps1 scrit creates self-contained deployments per OS and CPU architecture.

# Setup instructions
## Linux and MacOS
````
chmod +x startagent.sh
./startagent.sh <ARCHITECTURE_DIR> <DEVICE_CONNECTION_STRING> <other arguments...>
````

## Windows
````
./startagent.bat <ARCHITECTURE_DIR> <DEVICE_CONNECTION_STRING> <other arguments...>
````

For example
````
startagent.bat win-x64 "HostName=myiothub.azure-devices.net;DeviceId=mydevice;SharedAccessKey=mykey"  --option1 value1 --option2 value2
````