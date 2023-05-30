variable "env" {
  type        = string
  description = "The environment to assign the resources"
  default     = "dev"
}

variable "location" {
  type        = string
  description = "The resource phisical location"
  default     = "west europe"
}

variable "ip-pfx" {
  type        = string
  description = "Prefix of k8s vnet ip address space"
  default     = "22" # For dev
}