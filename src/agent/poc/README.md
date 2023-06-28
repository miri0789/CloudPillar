
This C# .NET Core device-side agent implementation listens for Cloud-to-Device messages and writes the received chunks to the corresponding files. It can be paused and restarted using HTTP GET requests on `http://localhost:8099`, and only one instance of the agent can run at a time using a global mutex.

JNJ IoT Agent .Net Core main principles:
1. It will not access Blob storage directly at all - lets assume the protocols used by the blob storage are absolutely blocked by the firewalls in the target environment.
2. Fault tolerant. E.g. It sends an D2C event to the backend side: the FirmwareUpdate Ready, with the filename and the chunk size in the payload, and the device_id in the custom property. The chunk size is deduced from the protocol being used: if it's MGTT, the chunk size will be optimized for MQTT, if its AMQP, the size will match AMQP best chunk size. Then it gets to a generic loop where it may receive different C2D messages with chunks in different order, of different files (many updates files at a time). It extracts the chunk id, size and the file name, and writes the chunk into the correct file at the correct position.
3. It will have file handles/streams lazy cached, and will not do unnecessary file opens if it already has this file handle open
4. It will be possible to completely pause the Agent, to allow the device to maximize its performance without CPU and I/O interruptions, and then restart it again
5. It will receive pause and restart commands via http://localhost:8099  GET requests
6. It will enforce only one instance of the Agent app is running on the device, via a global mutex. This one should adjust to both Windows and Linux cases.

# Low level design assumptions
0. back ground proccessing
1. Files upload & download - streaming manager - zip wildcards & incremental
2. Device twin listener
3. twin action executor 
4. actions classes
5. send azure messages
6. state machine 
7. api listener, support rest + api -returns device twin state (html & json)
8. schema validation - of api - find id of schema in device twin - and send message to beckand for getting schema and cache it (RAM)
9. schema validation - of messages
10. signature
11. configuration - dynamic
12. run time windows service 
13. run time docker container
14. controllable stream (with cancelation token)
15. installer
16. installer packaging pipeline
17. logger



To run a singleton container on a target host, use the following command:
```
docker run --name jnjiotagent --restart unless-stopped -d -p 8099:8099 -e DEVICE_CONNECTION_STRING="your_connection_string_here" jnjiotagent:0.0.1
```

To stop, run
```
docker stop jnjiotagent
```

Configure the Docker service on your Windows host to start automatically. To do this, open the Services management console (services.msc) or run the following command in an elevated PowerShell prompt:
```
Set-Service -Name docker -StartupType Automatic
```

On Linux:
```
sudo systemctl enable docker
```