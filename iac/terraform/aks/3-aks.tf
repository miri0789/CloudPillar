resource "azurerm_kubernetes_cluster" "aks" {
 name                    = "cp-${var.env}-aks"
  location                = azurerm_resource_group.rg.location
  tags                    = {"Terraform": "true"}
  resource_group_name     = azurerm_resource_group.rg.name
  dns_prefix              = "cp-${var.env}-dns"
  sku_tier                = "Free"
  kubernetes_version      = "1.25.6"
  private_cluster_enabled = var.env != "dev"
  
  default_node_pool {
    name                  = "agentpool"
    vm_size               = "Standard_D4s_v3"
    zones                 = [ "1", "2", "3" ]
    enable_auto_scaling   = true
    min_count             = 1
    max_count             = 5
    enable_node_public_ip = false
    os_disk_type          = "Managed"
    os_sku                = "Ubuntu"
    vnet_subnet_id        = azurerm_subnet.aks.id
  }
  
  network_profile {
        network_plugin    = "azure"
        load_balancer_sku = "standard"
        load_balancer_profile {
           managed_outbound_ip_count = 1
    }
    service_cidr       = "${var.ip-pfx}.0.0.0/16"
    dns_service_ip     = "${var.ip-pfx}.0.0.10"
    outbound_type      = "loadBalancer"
  }

  local_account_disabled = false
     
  identity {
    type = "SystemAssigned"
  }

  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.aks.id
  }
}
