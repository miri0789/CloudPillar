# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 
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
## IoT Agent State Transitions
::: mermaid
stateDiagram-v2


    [*] --> BUSY
    BUSY --> READY: sdk_cmdready when no_patch_no_base
    BUSY --> READY_WITHPATCH: sdk_cmdready when has_patch
    BUSY --> INTRANSIT_PATCH: sdk_cmdready when has_partpatch
    BUSY --> READY_WITHBASE: sdk_cmdready when has_base
    BUSY --> INTRANSIT_BASE: sdk_cmdready when has_partbase
    READY --> INTRANSIT_PATCH: cloud_cmdupgrade
    READY --> BUSY: sdk_cmdbusy
    INTRANSIT_PATCH --> READY_WITHPATCH
    INTRANSIT_PATCH --> BUSY: sdk_cmdbusy
    READY_WITHPATCH --> BUSY: sdk_cmdbusy
    READY_WITHPATCH --> WAIT_CONFIRM_PATCH
    WAIT_CONFIRM_PATCH --> APPLYING_PATCH: sdk_confirmpatch
    WAIT_CONFIRM_PATCH --> BUSY: sdk_cmdbusy
    APPLYING_PATCH --> INTRANSIT_BASE: integrity_fault
    APPLYING_PATCH --> READY: integrity_ok
    INTRANSIT_BASE --> READY_WITHBASE
    INTRANSIT_BASE --> BUSY: sdk_cmdbusy
    READY_WITHBASE --> WAIT_CONFIRM_BASE
    WAIT_CONFIRM_BASE --> APPLYING_BASE: sdk_confirmbase, boot_fwupd
    WAIT_CONFIRM_BASE --> BUSY: sdk_cmdbusy
    READY_WITHBASE --> BUSY: sdk_cmdbusy
    APPLYING_BASE --> READY: boot_sys
:::

2 types of updates are supported:
1. Partial update - a **PATCH**
The **Patch** update consists of the following optional components:
a. **Hosting App** binary and configuration files. Can define a followup action by the **Agent**.
b. Assignment of Windows build tag, for pulling the via the Windows Update. The assignment of the build is done at the **backend**, and triggered by the **Agent**.
c. Assignment of Windows Update-supported 3rd party applications, such as Antivirus. As in b.
d. App binary and configuration files of one or more 3rd party applications. As in a.
e. Specs and connections strings for IoT, Windows Update, and Log Collection Backends.
f. General scripts and automation actions.
2. Full _baseline_ update of the firmware - a **BASE**. Baseline firmware update on Windows machines contains snapshot of the system boot partition, as also additional optional data partitions, according to the Agent configuration.
The Base update is not necessary when the Patch is applied properly, without any integrity problems, so it is present only as a fallback.

## Architecture - principal flow
::: mermaid
flowchart 
%%{init: {'themeVariables': { 'fontSize': '18px', 'fontFamily': 'Inter'}}}%%
  classDef transparentSubgraph fill:transparent,stroke:#333,stroke-width:2px;
  classDef invisibleNode fill:transparent,stroke:transparent;

  subgraph Device_Hosting_Machine["Device Hosting Machine - Windows Pro, Enterprise or Server, or Linux machines"]
    InvisibleNode1[ ]:::invisibleNode
    subgraph Local_Service_Daemon["Docker Container"]
      IoT_Agent[IoT Agent]
      DotNetCore[.Net Core Runtime]
    end

    subgraph Hosting_Application["Hosting App - Carto, Velys"]
      SDK
    end

    Windows_Update[Windows Update Service]
    Azure_Monitor_Agent[Azure Monitoring Agent]
  end

  subgraph Azure_Availability_Zone["&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbspAzure Region"]
    subgraph Azure_Subscription[Azure Tenant Subscription]
      InvisibleNode3[ ]:::invisibleNode
      subgraph Azure_IoT_Hub[Azure IoT Hub]
        Partition[Partition #N]
        IoT_Device[IoT Device]
      end

      subgraph AKS_Backend_App[AKS Backend App]
        Firmware_Updates[Firmware Updater μSvc]
        Listen_D2C[D2C Listener μSvc]
      end

      subgraph Azure_Storage_Account[Storage Account]
        Binaries[Firmware Blobs]
        Storage_Container[(Storage Container)]
      end

      subgraph Azure_Log_Analytics[Log Analytics Workspace]
        Dashboard[[ALA Dashboard]]
      end

      subgraph Azure_Automation[Azure Automation]
        AA_Update_Management[Update Management]
      end
    end

    subgraph Atlas_Tennant_Account[Atlas Tenant Account]
      Version_Logs_Telemetry[Versions, Logs, Telemetry]
      MongoDB_Atlas[(MongoDB)]
      MongoDB_GraphQL([Managed GraphQL app])
      MongoDB_ChartsViews[[BI Charts Views]]
    end

    subgraph Intune_Tennant_Account[Log Collection Backend 1]
      direction RL
      Intune_Audit_Logs[(Audit Logs)]
      Intune_Operation_Logs[(Operation Logs)]
      Intune_Device_Compliance[(Device Compliance)]
      Intune_Device_Fleet[("Devices (Windows)")]
    end
  end

  Azure_Availability_Zone:::transparentSubgraph
  SDK -->|get_state| IoT_Agent
  SDK -->|commands| IoT_Agent
  IoT_Agent --> Partition
  IoT_Agent --o |pull C2D messages| IoT_Device
  Partition --> |deliver D2C messages| Listen_D2C
  Listen_D2C --> |store logs| Version_Logs_Telemetry
  Listen_D2C --> |start update| Firmware_Updates
  Firmware_Updates --> |1. read chunk| Binaries
  Firmware_Updates --> |2. push C2D messages| IoT_Device
  MongoDB_GraphQL --> Version_Logs_Telemetry
  MongoDB_ChartsViews --> Version_Logs_Telemetry
  Dashboard --> |query data| MongoDB_GraphQL
  Dashboard -.-> |embed charts| MongoDB_ChartsViews
  Dashboard -----> Intune_Audit_Logs
  Dashboard -----> Intune_Operation_Logs
  Dashboard -----> Intune_Device_Compliance
  Dashboard -----> Intune_Device_Fleet
  Azure_Monitor_Agent --Send logs--> Intune_Tennant_Account
  Windows_Update --> AA_Update_Management
  InvisibleNode1
  InvisibleNode3
:::

The J&J IoT Device **SDK** resides in the boundaries of the **hosting application**. It communicates to the IoT **Agent**, residing at the same Windows or Linux **machine**, as an always-on local service/daemon, deployed as a Docker container; the **SDK** sends Commands to the **Agent**, and the latter responds with state_changed notifications, according to the state transitions described [above](#state-transitions) (the notifications are delivered to the **SDK** via HTTP requests / polling).

The **Agent** communicates with **Azure IoT Hub** in a single-tenant, dedicated environment, sending D2C and accepting C2D messages from the backed. Behind the IoT Hub, there's a k8s **backend app**, listening for D2C messages in a particular **partition** #N, and sending firmware updates via C2D messages, streaming them as chunks. The **Agent** also sends logs to the backend **D2CListener** microservice. Then, all version, logs and telemetry is stored in **MongoDB Atlas**. 

Firmware binaries are kept in Azure **storage container**.

**Log Collection Backend 1** is one or more of the following:
* Azure Automation
* Intune
* Cloud Endpoint Management
* Azure IoT Device Update



**Service Mesh**: Internal AKS microservices S2S authentication and load-balancing is governed by [Azure AKS Open Service Mesh addon](https://learn.microsoft.com/en-us/azure/aks/open-service-mesh-about)
When using a service mesh, we can enable scenarios such as:
* Encrypting all traffic in cluster: Enable mutual TLS between specified services in the cluster. This can be extended to ingress and egress at the network perimeter and provides a secure-by-default option with no changes needed for application code and infrastructure.
* A/B, Blue-Green, Canary and phased rollouts: Specify conditions for a subset of traffic to be routed to a set of new services in the cluster. On successful test of canary release, remove conditional routing and phase gradually increasing % of all traffic to a new service. Eventually, all traffic will be directed to the new service.
* Traffic management and manipulation: Create a policy on a service that rate limits all traffic to a version of a service from a specific origin, or a policy that applies a retry strategy to classes of failures between specified services. Mirror live traffic to new versions of services during a migration or to debug issues. Inject faults between services in a test environment to test resiliency.
* Observability: Gain insight into how your services are connected and the traffic that flows between them. Gather metrics, logs, and traces for all traffic in the cluster, including ingress/egress. Add distributed tracing abilities to applications.

**Autoscaling**: The production deployment is auto-scaleable from the start, using K8S [HPAs](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/) and [KEDA](https://learn.microsoft.com/en-us/azure/aks/keda-about).

