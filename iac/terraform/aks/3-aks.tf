resource "azurerm_kubernetes_cluster" "aks" {
  name                    = "iot-${var.env}-aks"
  tags                    = { "Terraform": "true" }
  resource_group_name     = azurerm_resource_group.aks.name
  location                = azurerm_resource_group.aks.location
  private_cluster_enabled = var.env != "lab"
  dns_prefix              = "iot-${var.env}-dns"
  sku_tier                = "Free"
  kubernetes_version      = "1.26.3"
  open_service_mesh_enabled = true
  default_node_pool {
    name                  = "agentpool"
    node_count            = 2
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
    service_cidr          = "${var.ip-pfx}.0.0.0/16"
    dns_service_ip        = "${var.ip-pfx}.0.0.10"
    docker_bridge_cidr    = "172.17.0.1/16"
    outbound_type         = "loadBalancer"
  }

  local_account_disabled = false
     
  identity {
    type = "SystemAssigned"
  }

  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.aks.id
  }
}
