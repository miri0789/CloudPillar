resource "azurerm_log_analytics_workspace" "aks" {
  name                = "cp-${var.env}-log-analytics"
  location            = var.location
  resource_group_name = azurerm_resource_group.aks.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}
resource "azurerm_kubernetes_cluster" "aks" {

  depends_on = [
    azurerm_virtual_network.aks
  ]
  
  name                    = "cp-${var.env}-aks"
  location                = var.location
  tags                    = {"Terraform": "true"}
  resource_group_name     = azurerm_resource_group.aks.name
  dns_prefix              = "cp-${var.env}-dns"
  sku_tier                = "Free"
  kubernetes_version      = "1.25.6"
  private_cluster_enabled = var.env != "dev" && var.env != "tst"
  open_service_mesh_enabled = true
  
  default_node_pool {
    name                  = "agentpool"
    vm_size               = "Standard_B2s_v2"
    zones                 = [ "1", "2", "3" ]
    enable_auto_scaling   = true
    min_count             = 1
    max_count             = 5
    enable_node_public_ip = false
    os_disk_type          = "Managed"
    os_sku                = "Ubuntu"
    vnet_subnet_id        = azurerm_subnet.aks.id
  }
    
  role_based_access_control_enabled = true

  network_profile {
        network_plugin    = "azure"
        load_balancer_sku = "standard"
        load_balancer_profile {
           managed_outbound_ip_count = 1
    }
    service_cidr       = "10.23.1.0/24"
    dns_service_ip     = "10.23.1.10"
    outbound_type      = "loadBalancer"
  }

  local_account_disabled = false
     
  identity {
    type = "SystemAssigned"
  }

  azure_policy_enabled = true
  
  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.aks.id
  }
}