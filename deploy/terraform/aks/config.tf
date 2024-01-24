data "azurerm_app_service_certificate_order" "cert" {
   name                = "${var.env}be-cloudpillar-net"
   resource_group_name = "cp-iot-${var.env}-rg"
}

resource "null_resource" "copy-templates" {
  provisioner "local-exec" {
    command = <<-EOT
        Copy-Item -Path "..\..\backend\yamls\AzureKeyVaultSecret.yaml" -Destination "." -Recurse
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
}

resource "null_resource" "add-namespaces" {
  provisioner "local-exec" {
    command = <<-EOT
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl create namespace akv2k8s'
    EOT
    interpreter = ["PowerShell", "-Command"]
  }
}

resource "null_resource" "akv-secret" {
    provisioner "local-exec" {
    command = <<-EOT
        
        (Get-Content AzureKeyVaultSecret.yaml) -replace "{env}", "${var.env}" | Set-Content -Path 'AzureKeyVaultSecret.yaml' -encoding utf8
        (Get-Content AzureKeyVaultSecret.yaml) -replace "{cert}", "${data.azurerm_app_service_certificate_order.cert.certificates[0].key_vault_secret_name}" | Set-Content -Path 'AzureKeyVaultSecret.yaml' -encoding utf8
        az aks get-credentials --name cp-${var.env}-aks --resource-group cp-ms-${var.env}-rg
        kubectl apply -f AzureKeyVaultSecret.yaml
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [null_resource.copy-templates, null_resource.add-namespaces ]
}

resource "null_resource" "helm-repos-updates" {
    provisioner "local-exec" {
    command = <<-EOT
        az aks get-credentials --name cp-${var.env}-aks --resource-group cp-ms-${var.env}-rg
        helm repo add spv-charts https://charts.spvapi.no
        helm repo update
        helm upgrade --install akv2k8s spv-charts/akv2k8s --namespace akv2k8s
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [null_resource.add-namespaces, null_resource.akv-secret, null_resource.copy-templates ]
}

resource "null_resource" "remove-temaple-files" {
  provisioner "local-exec" {
    command = <<-EOT
       Remove-Item -path AzureKeyVaultSecret.yaml
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [null_resource.akv-secret, null_resource.helm-repos-updates]
}
