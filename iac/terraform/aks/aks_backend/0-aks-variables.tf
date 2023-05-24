
variable "tenant_id" {
    type = string
    description = "The IaC Backend Tenant ID"
}
variable "subscription_id" {
    type = string
    description = "The IaC Backend Subscription ID"
}
variable "client_id" {
    type = string
    description = "The IaC Backend Client ID"
}
variable "client_secret" {
    type = string
    description = "The IaC Backend Client Secret"
}
variable "Location" {
    type = string
    description = "The IaC Backend Location"
}
variable "region" {
    type = string
    description = "The IaC Backend Region"
}
variable "tf_backend_rg" {
    type = string
    description = "The IaC Backend Resource Group Name"
}
variable "tf_backend_sa" {
    type = string
    description = "The IaC Backend Storage Account Name"
}
variable "tf_backend_container" {
    type = string
    description = "The IaC Backend Container Name"
}
variable "tfstate_key" {
    type = string
    description = "The IaC Backend State File Name"
}
variable "tf_backend_kv" {
    type = string
    description = "The IaC Backend Key Vault Name"
}

variable "aks_rg" {
    type        = string
    description = "The name of the resource group in which to create the resources"
}

variable "aks_vnet" {
    type        = string
    description = "The name of the virtual network in which to create the resources"
}

variable "aks_subnet" {
    type        = string
    description = "The name of the subnet in which to create the resources"
}

#* New Resources

#* IaC Backend Resources

variable "devops_url" {
    type        = string
    description = "The name of the subnet in which to create the resources"
    default = "https://dev.azure.com/BiosenseWebsterIs"
}
variable "agent_pool" {
    type        = string
    description = "The name of the subnet in which to create the resources"
    /* default = "IoT-Dicom-Pool */
}


variable "personal_access_token_secret" {
    type        = string
    description = "The name of the subnet in which to create the resources"
    sensitive = true
    /* default = "IoT-Dicom-Agent-Devops-Token" */
}

variable "personal_access_token_value" {
    type        = string
    description = "The name of the subnet in which to create the resources"
    sensitive = true
}


# +N IaC Resource Group
variable "aks_backend_rg" {
    type = string
    description = "The IaC Backend Resource Group Name"
}


variable "aks_ssh_user" {
    type = string
    description = "The User name for the SSH Connection"
}


variable "aks_backend_vm_public_ssh_secret_name" {
    type        = string
    description = "The name of the secret in which to create the resources"
}

variable "aks_backend_vm_private_ssh_secret_name" {
    type        = string
    description = "The name of the secret in which to create the resources"
}

variable "aks_backend_vm_nic_name" {
    type        = string
    description = "The name of the network interface card in which to create the resources"
}
variable "aks_backend_vm_name" {
    type        = string
    description = "The name of IaC Backend VM in which to create the resources"
}

variable "aks_agent_name" {
    type        = string
    description = "The name of the Self Hosted Agent"
}


variable "aks_backend_kv" {
    type        = string
    description = "The name of the Key Vault in which to create the resources"
}