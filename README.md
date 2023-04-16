# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references

# Build and Test
1. Purge all  C2D messages
```
az iot hub invoke-device-method --device-id <your_device_id> --hub-name <your_iothub_name> --method-name cloudtodevicepurge
```

TODO: Describe and show how to build your code and run the tests. 

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)

:::mermaid
mindmap
  root((IoT DICOM Hub))
    Objectives
      Missions
      ::icon(fa fa-book)
        Provide IoT management bus
        Support diverse and sparsely connected environments
        Provide DICOM transfer hub and management
        Comply with the relevant regulations
        Leverage latest and greatest in IoT
      Cornerstones
        Principles
          A first class<br/>resident of IOTH
          Minimalism
          Synergy with CartoNet
        Stack
          )Targets Azure(
          Basing on Azure IoT Hub
            ((MQTT))
            (AMQP)
            [HTTP]
            mTLS
            RDP
            SSH
            MS_WUSP
          Programming platforms
            C#
            Python
            C
            Node*
            Angular<br/>or React            
          Considered 3rd parties
            Azure IoT Edge<br/>turned down
            Azure IoT Device Update<br/>potential
            Windows Update
            DataDog<br/>turned down
            Ngrok
    Top Cases
      Fleet registration<br/>and management
      Service pack download<br/>and setup
      Logs upload and analysis
    Scope
      What is included
        MVP Scope
          SP Management
          Log Collector
          Authenticaction Broker
          Single_Tenant Saas
            Self_provisioning
      What is excluded
        NOT a DMS
        NOT an ISRM
        NOT a platform
        NOT Auth Provider
        NOT multi_tenant SaaS
        NOT a BI 
    Timeline
      PoC
      MVP Scope
      GA #1 F&F
      GA #2
      Extended Scope
      Maintenance
:::

