resource "azurerm_kubernetes_cluster" "aks" {
  name                    = "iot-${var.env}-aks"
  tags                    = { "Terraform": "true" }
  resource_group_name     = azurerm_resource_group.aks.name
  location                = azurerm_resource_group.aks.location
  private_cluster_enabled = true
  dns_prefix              = "iot-${var.env}-dns"
  sku_tier                = "Free"
  kubernetes_version      = "1.24.9"
  
  default_node_pool {
    name                  = "agentpool"
    #node_count            = 2
    vm_size               = "Standard_D4s_v3"
    availability_zones    = ["1","2", "3" ]
    max_count             = 5
    min_count             = 1
    max_pods              = 110
    enable_auto_scaling   = true
    enable_node_public_ip = false
    os_disk_type          = "Managed"
    os_sku                = "Ubuntu"
    vnet_subnet_id        = azurerm_subnet.aks.id
  }
  
  # role_based_access_control_enabled = true

  network_profile {
    network_plugin        = "azure"
    load_balancer_sku     = "Standard"
    load_balancer_profile {
      managed_outbound_ip_count = 1
    }
    service_cidr          = "22.0.0.0/16"
    dns_service_ip        = "22.0.0.10"
    docker_bridge_cidr    = "172.17.0.1/16"
    outbound_type         = "loadBalancer"
  }

  local_account_disabled = false
     
  identity {
    type = "SystemAssigned"
  }

  # azure_policy_enabled = true
  # ingress_application_gateway {
  #   gateway_id = azurerm_application_gateway.appgw.id
  # }
  
  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.aks.id
  }
}

# data "azurerm_container_registry" "aks" {
#   provider            = azurerm.prod
#   name                = "velys-acr"
#   resource_group_name = "velys-acr"
# }

resource "azurerm_role_assignment" "aks" {
  principal_id                     =  azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.aks.id
  skip_service_principal_aad_check = true
}
