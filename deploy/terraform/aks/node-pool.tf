
resource "azurerm_kubernetes_cluster_node_pool" "aks" {
  name                  = "spotcpunp"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.aks.id
  vm_size               = "Standard_D2s_v3"
  zones                 = [ "1", "2", "3" ]
  node_count            = 3
  min_count             = 1
  max_count             = var.env == "dev" || var.env == "tst" ? 5 : 20
  enable_auto_scaling   = true
  enable_node_public_ip = false
  os_disk_type          = "Managed"
  os_sku                = "Ubuntu"
  vnet_subnet_id        = azurerm_subnet.aks.id
  node_taints           = []
  priority              = "Spot"
  eviction_policy       = "Delete"

  # Ignore changes to the specified attributes
  lifecycle { ignore_changes = [node_count, node_taints] }
}
