// Grant k8s permissions to registry
resource "azurerm_role_assignment" "aks" {
  principal_id                     =  azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = data.azurerm_container_registry.aks.id
  skip_service_principal_aad_check = true
}
