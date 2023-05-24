resource "kubernetes_namespace" "aks" {
  metadata {
    # annotations = {
    #   name = "example-annotation"
    # }
    # labels = {
    #   mylabel = "label-value"
    # }
    name = "jnj-iot-osm-ns"
  }
}
