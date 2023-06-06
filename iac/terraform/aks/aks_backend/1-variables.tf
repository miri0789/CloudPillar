
variable "tenant_id" {
    type = string
    description = "The IaC Backend Tenant ID"
    sensitive = true
}
variable "subscription_id" {
    type = string
    description = "The IaC Backend Subscription ID"
    sensitive = true

}
variable "client_id" {
    type = string
    description = "The IaC Backend Client ID"
    sensitive = true
}
variable "client_secret" {
    type = string
    description = "The IaC Backend Client Secret"
    sensitive = true
}

variable "env" {
    type = string
    description = "The Azure Environment (prd, dev, tst, stg)"
    default = "dev"
}
variable "location" {
    type = string
    description = "The IaC Backend Location"
    default = "West Europe"
}
variable "region" {
    type = string
    description = "The IaC Backend Location"
    default = "westeurope"
}

variable "tf_backend_rg" {
    type = string
    description = "The IaC Backend Resource Group"
    sensitive = true
}

variable "tf_backend_sa" {
    type = string
    description = "The IaC Backend Storage Account"
    sensitive = true
}

variable "tf_backend_kv" {
    type = string
    description = "The IaC Backend Key Vault"
    sensitive = true
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
    default = "Cloud-Pillar-Pool"
}


variable "personal_access_token_secret" {
    type        = string
    description = "The name of the subnet in which to create the resources"
    sensitive = true
    /* default = "Cloud-Pillar-Agent-Token" */
    default = "iot-dicom-agent-devops-token"
}

variable "personal_access_token_value" {
    type        = string
    description = "The name of the subnet in which to create the resources"
    sensitive = true
}

variable "ip-pfx" {
    type        = string
    description = "The name of the subnet in which to create the resources"
    default = 22
}