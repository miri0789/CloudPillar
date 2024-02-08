
data "azurerm_subscription" "current" {}

locals{ 
    subscription-id = split("/", data.azurerm_subscription.current.id)[2]
    host = "${var.env}be-cloudpillar-net"
}

resource "null_resource" "copy-templates" {
  provisioner "local-exec" {
    command = <<-EOT
        Copy-Item -Path "templates\*" -Destination "." -Recurse
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
}

resource "null_resource" "add-namespaces" {
  provisioner "local-exec" {
    command = <<-EOT
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl create namespace cp-be-ns'
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl label namespace cp-be-ns openservicemesh.io/monitored-by=osm'
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl annotate namespace cp-be-ns openservicemesh.io/sidecar-injection=enabled'
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks"-c 'kubectl create namespace akv2k8s'
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl create namespace traefik'
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl label namespace traefik openservicemesh.io/monitored-by=osm'
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl annotate namespace traefik openservicemesh.io/sidecar-injection=enabled'
        az aks command invoke -g cp-ms-${var.env}-rg -n "cp-${var.env}-aks" -c 'kubectl create namespace cp-dns'
    EOT
    interpreter = ["PowerShell", "-Command"]
  }
}

resource "null_resource" "akv-secret" {
  
    provisioner "local-exec" {
    command = <<-EOT
        az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f traefik.toml -c 'kubectl create configmap traefik-config --from-file=traefik.toml -n traefik'
        (Get-Content AzureKeyVaultSecret.yaml) -replace "{env}", "${var.env}" | Set-Content -Path 'AzureKeyVaultSecret.yaml' -encoding utf8
        (Get-Content AzureKeyVaultSecret.yaml) -replace "{cert}", "${azurerm_app_service_certificate_order.app-cert.certificates[0].key_vault_secret_name}" | Set-Content -Path 'AzureKeyVaultSecret.yaml' -encoding utf8
        az aks get-credentials --name cp-${var.env}-aks --resource-group cp-ms-${var.env}-rg
        kubectl apply -f AzureKeyVaultSecret.yaml

        (Get-Content generatecertificate.ps1) -replace "{env}", "${var.env}" | Set-Content -Path 'generatecertificate.ps1' -encoding utf8
        PowerShell -ExecutionPolicy Bypass -File .\generatecertificate.ps1
        $fileContentBytes = Get-Content -Path 'certificate.pfx' -Encoding Byte
        $base64String = [System.Convert]::ToBase64String($fileContentBytes)
        az keyvault secret set --vault-name cp-${var.env}be-kv --name ${var.env}be-default-cloudpillar-net --value $base64String --description "application/x-pkcs12"
        Remove-Item -Path certificate.pfx -Force

        (Get-Content AzureKeyVaultSecret.yaml) -replace "${azurerm_app_service_certificate_order.app-cert.certificates[0].key_vault_secret_name}", "${var.env}be-default-cloudpillar-net" | Set-Content -Path 'AzureKeyVaultSecret.yaml' -encoding utf8
        (Get-Content AzureKeyVaultSecret.yaml) -replace "${var.env}be-cert", "${var.env}be-default-cert" | Set-Content -Path 'AzureKeyVaultSecret.yaml' -encoding utf8
        kubectl apply -f AzureKeyVaultSecret.yaml

        (Get-Content values-traefik.yaml) -replace "{host}", "${local.host}" | Set-Content -Path 'values-traefik.yaml' -encoding utf8
        $location=$((az group show -n cp-ms-${var.env}-rg | ConvertFrom-Json).location)
        $clientid=$((az identity show -g MC_cp-ms-${var.env}-rg_cp-${var.env}-aks_$($location) -n cp-${var.env}-aks-agentpool | ConvertFrom-Json).clientId)
        (Get-Content values-akv2k8s.yaml) -replace "{msi}", "$($clientid)" -replace "{tenant}", "${data.azuread_client_config.current.tenant_id}" -replace "{subscription}", "${data.azurerm_subscription.current.display_name}" | Set-Content -Path 'values-akv2k8s.yaml' -encoding utf8
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [azurerm_app_service_certificate_order.app-cert, null_resource.copy-templates, null_resource.add-namespaces, null_resource.helm-repos-updates ]
}

# resource "null_resource" "dns-cred" {
#     provisioner "local-exec" {
#     command = <<-EOT
#         $jsonStr = @'
#         {
#           "tenantId": "${data.azuread_client_config.current.tenant_id}",
#           "subscriptionId": "${local.subscription-id}",
#           "resourceGroup": "cp-ms-${var.env}-rg",
#           "aadClientId": "${data.azuread_application.int-cyber.client_id}",
#           "aadClientSecret": "${data.azurerm_key_vault_secret.dns-secret.value}"
#         }
#         '@
#         $jsonObj = $jsonStr | ConvertFrom-Json
#         $jsonObj | ConvertTo-Json | Out-File -FilePath "azure.json"
#         az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -c 'kubectl create secret generic dns-update-cred --from-file=azure.json=azure.json -n cp-dns' -f .
#     EOT
#     interpreter = ["PowerShell", "-Command"]
#     working_dir = "${path.module}"
#   }
#   depends_on = [ null_resource.add-namespaces, null_resource.copy-templates ]
# }
# 
resource "null_resource" "helm-repos-updates" {
    provisioner "local-exec" {
    command = <<-EOT
        az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f . -c 'helm repo add spv-charts https://charts.spvapi.no && helm upgrade --install akv2k8s spv-charts/akv2k8s --namespace akv2k8s --values values-akv2k8s.yaml --debug'
        az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f . -c 'helm repo add bitnami https://charts.bitnami.com/bitnami && helm upgrade --install externaldns bitnami/external-dns --values values-cpdns.yaml --set provider=azure --set azure.secretName=dns-update-cred --set azure.secretNamespace=cp-dns --set domainFilters[0]=cloudpillar.net --namespace cp-dns'
        az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f . -c 'helm repo add traefik https://traefik.github.io/charts && helm upgrade --install traefik traefik/traefik -n traefik --values values-traefik.yaml --debug'
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [null_resource.add-namespaces, null_resource.akv-secret, null_resource.copy-templates ]
}

resource "null_resource" "mesh-ips" {
    provisioner "local-exec" {
    command = <<-EOT
      (Get-Content patch-file.yaml) -replace "{aks-ip}", "${var.addressSpace}" | Set-Content -Path 'patch-file.yaml' -encoding utf8
      az aks get-credentials --name cp-${var.env}-aks --resource-group cp-ms-${var.env}-rg
      az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f . -c 'kubectl patch meshconfig osm-mesh-config -n kube-system -o json --type=merge --patch-file patch-file.yaml'
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [null_resource.copy-templates ]
}

resource "null_resource" "mesh-ips" {
    provisioner "local-exec" {
    command = <<-EOT
        az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f . -c 'helm repo add spv-charts https://charts.spvapi.no && helm upgrade --install akv2k8s spv-charts/akv2k8s --namespace akv2k8s --values values-akv2k8s.yaml --debug'
        az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f . -c 'helm repo add bitnami https://charts.bitnami.com/bitnami && helm upgrade --install externaldns bitnami/external-dns --values values-cpdns.yaml --set provider=azure --set azure.secretName=dns-update-cred --set azure.secretNamespace=cp-dns --set domainFilters[0]=cloudpillar.net --namespace cp-dns'
        az aks command invoke -g cp-ms-${var.env}-rg -n cp-${var.env}-aks -f . -c 'helm repo add traefik https://traefik.github.io/charts && helm upgrade --install traefik traefik/traefik -n traefik --values values-traefik.yaml --debug'
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [null_resource.add-namespaces, null_resource.copy-templates ]
}

resource "null_resource" "remove-temaple-files" {
  provisioner "local-exec" {
    command = <<-EOT
       Remove-Item -path AzureKeyVaultSecret.yaml
       Remove-Item -path generatecertificate.ps1
       Remove-Item -path values-traefik.yaml
       Remove-Item -path values-akv2k8s.yaml
       Remove-Item -path values-cpdns.yaml
       Remove-Item -path traefik.toml
    EOT
    interpreter = ["PowerShell", "-Command"]
    working_dir = "${path.module}"
  }
  depends_on = [null_resource.akv-secret, null_resource.helm-repos-updates]
}
