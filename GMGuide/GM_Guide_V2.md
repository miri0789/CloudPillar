# Gal Knowledge Transfer Guide

## Table of Contents
- [Gal Knowledge Transfer Guide](#gal-knowledge-transfer-guide)
  - [Table of Contents](#table-of-contents)
  - [📘 1. Introduction](#-1-introduction)
    - [🎯 1.1 Purpose of This Document](#-11-purpose-of-this-document)
    - [🏗 1.2 Focused Infrastructure Components](#-12-focused-infrastructure-components)
    - [👥 1.3 Target Audience](#-13-target-audience)
  - [🛠 2. Prerequisites](#-2-prerequisites)
    - [🌐 2.1 Backend Infrastructure Prerequisites](#-21-backend-infrastructure-prerequisites)
      - [2.1.1 📦 Azure Resource Group](#211--azure-resource-group)
      - [2.1.2 🗄 Azure Storage Account](#212--azure-storage-account)
      - [2.1.3📂 Azure Storage Container](#213-azure-storage-container)
      - [2.1.4🔒 Azure Key Vault](#214-azure-key-vault)
      - [2.1.5👤 Service Principal Creation](#215-service-principal-creation)
      - [2.1.6🏊 Azure DevOps Agent Pool](#216-azure-devops-agent-pool)
      - [2.1.7🎟 Azure DevOps Personal Access Token](#217-azure-devops-personal-access-token)
      - [2.1.8🗝 Azure Key Vault Secrets](#218-azure-key-vault-secrets)
      - [2.1.9🛡 Permission \& Credentials](#219-permission--credentials)
  - [3. Backend Infrastructure Creation](#3-backend-infrastructure-creation)
    - [3.1 Terraform Backend Creation"](#31-terraform-backend-creation)
      - [3.1.1 Azure Login \& Setting the Environment Variables](#311-azure-login--setting-the-environment-variables)
      - [3.1.2 Azure Resource Group Creation](#312-azure-resource-group-creation)
      - [3.1.3 Azure Storage Account \& Container Creation](#313-azure-storage-account--container-creation)
      - [3.1.4 Azure Service Principal Creation (With Owner Role Assignment and Client ID \& Client Secret Output)](#314-azure-service-principal-creation-with-owner-role-assignment-and-client-id--client-secret-output)

## 📘 1. Introduction

---

### 🎯 1.1 Purpose of This Document

This guide serves as a comprehensive manual for knowledge transfer before my departure. The document is structured into three main sections:

1. **Preparing the Environment**: Lays out the prerequisites and backend infrastructure requirements for successful code execution.
2. **Terraform Code**: Discusses the Terraform code used for creating the necessary infrastructure.
3. **Azure DevOps Pipelines**: Explains how the Azure DevOps pipelines are configured to execute the Terraform code and establish the infrastructure.

---

### 🏗 1.2 Focused Infrastructure Components

The infrastructure components covered in this guide include:

- Virtual Machines (VMs)
- Virtual Networks (Vnets)
- Subnets
- Network Security Groups (NSGs)
- Azure Container Registries (ACRs)
- Storage Accounts
- Vaults
- SSH Keys
* **Azure Devops**
  - Azure Devops Pipelines
  - Azure Devops Service Connections
  - Azure Devops Agent Pools
  - Azure Devops Self Hosted Agents
  - Azure Devops Variable Groups


These components are orchestrated to host Azure DevOps pipeline agents, which in turn build images and push them to a private Azure Container Registry (ACR) within the same subnet.

---

### 👥 1.3 Target Audience

This guide is tailored for DevOps Engineers who will assume the responsibilities of deploying and maintaining our Cloud Pillar CI/CD pipelines and infrastructure. While a foundational understanding of technologies like Terraform, Docker, Kubernetes, Azure, and Windows is assumed, this guide also accommodates those unfamiliar with Linux by providing detailed explanations of Linux commands.

## 🛠 2. Prerequisites

---

### 🌐 2.1 Backend Infrastructure Prerequisites 

---

#### 2.1.1 📦 Azure Resource Group
- **Purpose**: Container for managing and organizing Azure resources.

---

#### 2.1.2 🗄 Azure Storage Account
- **Purpose**: Account for storing Terraform state files and other data.

---

#### 2.1.3📂 Azure Storage Container
- **Purpose**: Specific container within the storage account for state files.

---

#### 2.1.4🔒 Azure Key Vault
- **Purpose**: Secure vault for storing sensitive information like secrets and keys.

---

#### 2.1.5👤 Service Principal Creation
- **Client ID**: Unique identifier for the service principal.
- **Client Secret**: Secure key for the service principal.
- **Role Assignment**: Owner role for resource management.

---

#### 2.1.6🏊 Azure DevOps Agent Pool
- **Purpose**: Pool of agents used for running pipeline tasks.

---

#### 2.1.7🎟 Azure DevOps Personal Access Token
- **Purpose**: Token for authenticating against Azure DevOps services.

---

#### 2.1.8🗝 Azure Key Vault Secrets
- **Azure Client ID**: Stored as a secret in the key vault.
- **Azure Client Secret**: Another secret stored in the key vault.
- **Azure DevOps PAT**: Personal Access Token stored as a secret.

---

#### 2.1.9🛡 Permission & Credentials
- **Azure DevOps Service Connection**: Connection settings for Azure services.
- **Azure DevOps Backend Secrets Vault**: Variable groups connected to Azure Key Vault for secure storage.
- **Service Principal Access to Key Vault and Storage Account**: Required permissions for accessing Azure Key Vault and Azure Storage Account.

## 3. Backend Infrastructure Creation

---

### 3.1 Terraform Backend Creation"

---

#### 3.1.1 Azure Login & Setting the Environment Variables 

- **Purpose**: Logs into Azure and sets the environment variables for Terraform.
- **Commands**:

```powershell
cd Scripts/Powershell
./1_azureLogin.ps1
```

```powershell
./2_azSetSub.ps1 -EnvName <env> (dev, test, prod)
```

---

#### 3.1.2 Azure Resource Group Creation
- **Purpose**: Creates the resource group for the Terraform backend.
- **Commands**:

```powershell
.\3_az_create_rg.ps1
```




#### 3.1.3 Azure Storage Account & Container Creation

- **Purpose**: Creates the storage account and container for storing Terraform state files.
- **Commands**:

```powershell
.\4_az_create_sa.ps1
```

---

#### 3.1.4 Azure Service Principal Creation (With Owner Role Assignment and Client ID & Client Secret Output)

- **Purpose**: Creates the service principal for authenticating against Azure services.
- **Commands**:

```powershell
.\5_az_create_sp.ps1
```


---





<!-- #### 3.1.3 Azure Key Vault Creation -->




1. Backend Infrastructure Prerequisites for each environment:
    1. Azure Resource Group
    2. Azure Storage Account
    3. Azure Storage Container
    4. Azure Key Vault
    5. Service Principal Creation
        1. Client ID
        2. Client Secret
        3. Role Assignment - Owner
    6. Azure DevOps Agent Pool
    7. Azure DevOps Personal Access Token
    8. Azure Key Vault Secrets
        1. Azure Client ID
        2. Azure Client Secret
        3. Azure DevOps Personal Access Token
    9. Permission & Credentials:
        1. Azure DevOps Service Connection
        2. Azure DevOps Backend Secrets Vault - Variable Groups
        3. Service Principal Access to Key Vault Secrets and Storage Account



3. Local Exectuion:
    1. Environment Variables File
        1. All.env File Creation - General Environment Variables File
            1. ARM_TENANT_ID
            2. LOCATION
            3. REGION
            4. DEVOPS_URL
            5. AGENT_POOL
            6. PAT_SECRET_NAME
        2. <env>.env File Configuration - Environment Specific Env Variables File
            1. ENV - <env>
            2. ARM_SUBSCRIPTION_ID
            3. ARM_SUBSCRIPTION_NAME
                1. Cloud Pillar <env>
            4. ARM_CLIENT_ID
                1. NAME - cp-tf-client-id-<env>
            5. ARM_CLIENT_SECRET
                1. NAME - cp-tf-client-secret-<env> 
            6. TF_BACKEND_RG
                1. cp-tf-backend-rg-<env>
            7. TF_BACKEND_SA
                1. cptfbackendsa<env>
            8. TF_BACKEND_CONTAINER
                1. tfstate
            9. TF_BACKEND_KV
                1. cp-tf-backend-kv-<env>
            10. SP_NAME
                1. cp-tf-sp-<env>       
            11. SP_ID
                1. cp-tf-sp-id-<env>       
    2. Software & Hardware Requirements
        1. Terraform
        2. Azure CLI
        3. Git
        4. Docker
        5. Kubernetes
        6. Kubectl
        7. Azure DevOps Agent Installation
        8. Azure DevOps Agent Configuration

4. Pipeline Execution:
    1. Azure DevOps Pipeline
        1. Parameters:
            1. env - <env>
            2. Action - <plan/apply/destroy>

        2. Pipeline Tasks
            1. Terraform Init
            2. Terraform Plan
            3. Terraform Apply
            4. Terraform Destroy

    2. Variable Groups
        1. Variable Group: arm-vg
            1. AGENT_POOL
            2. ARM_TENANT_ID
            3. DEVOPS_URL
            4. LOCATION
            5. REGION
            6. PAT_SECRET_NAME

        2. Variable Group: <env>-vg
            1. ARM_CLIENT_ID
            2. ARM_CLIENT_SECRET
            3. ARM_SUBSCRIPTION_ID
            4. ARM_SUBSCRIPTION_NAME
                1. Cloud Pillar <env>
            5. ENV
                1. <env>
            6. ACR_NAME
                1. cp<env>acr

        3. Variable Group: iac-backend-vg
            1. TF_BACKEND_RG
                1. cp-tf-backend-rg-<env>
            2. TF_BACKEND_SA
                1. cptfbackendsa<env>
            3. TF_BACKEND_CONTAINER
                1. tfstate
            4. TF_BACKEND_KV
                1. cp-tf-backend-kv-<env>

        4. Variable Group: backend-secrets-vg - Connected to Key Vault
            1. ARM_CLIENT_ID
                1. NAME - cp-tf-client-id-<env>
            2. ARM_CLIENT_SECRET
                1. NAME - cp-tf-client-secret-<env> 
            3. PAT
                1. NAME - cp-tf-backend-pat

    3. Software & Hardware Requirements
        1. Terraform
        2. Azure CLI
        3. Git
        4. Docker
        5. Kubernetes
        6. Kubectl
        7. Azure DevOps Agent Installation
        8. Azure DevOps Agent Configuration

5. Permission & Credentials:
    1. Azure DevOps Service Connection
    2. Azure DevOps Backend Secrets Vault - Variable Groups