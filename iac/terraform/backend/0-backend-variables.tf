
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
