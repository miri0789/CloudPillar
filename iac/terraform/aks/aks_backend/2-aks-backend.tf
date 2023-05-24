#* Terraform Provider Configurations
data "azurerm_client_config" "current" {}
data "azuread_client_config" "current" {}



#* Terraform Import Resources
data "azurerm_resource_group" "aks_backend" {
    name = var.aks_rg
}

data "azurerm_subnet" "aks_backend" {
    name                 = var.aks_subnet
    virtual_network_name = var.aks_vnet
    resource_group_name  = var.aks_rg
}

data "azurerm_virtual_network" "aks_backend" {
    name                 = var.aks_vnet
    resource_group_name  = var.aks_rg
}





resource "azurerm_resource_group" "aks_backend" {
    name     = var.aks_backend_rg
    location = var.Location
}


resource "tls_private_key" "aks_backend_ssh_key" {
    algorithm = "RSA"
    rsa_bits = 4096
}







#* Key Vaults

# +N IaC Key Vault
resource "azurerm_key_vault" "aks_backend" { 
    name = var.aks_backend_kv
    location = azurerm_resource_group.aks_backend.location
    resource_group_name = azurerm_resource_group.aks_backend.name
    tenant_id = data.azurerm_client_config.current.tenant_id
    
    sku_name = "standard"
    
    enabled_for_deployment = true
    enabled_for_disk_encryption = true
    enabled_for_template_deployment = true
    purge_protection_enabled    = false

    access_policy {
        tenant_id = data.azuread_client_config.current.tenant_id
        object_id = data.azuread_client_config.current.object_id
        secret_permissions = [
            "Get",
            "List",
            "Set",
            "Delete",
            "Backup",
            "Recover",
            "Restore",
            "Purge"
        ]
        key_permissions = [
            "Get",
            "List",
            "Sign",
            "Delete",
            "Backup",
            "Recover",
            "Restore",
            "Encrypt",
            "Decrypt",
            "UnwrapKey",
            "WrapKey",
            "Purge",
            "Verify"
        ]
        storage_permissions = [
            "Get",
            "List",
            "Delete",
            "Set",
            "Update",
            "Regeneratekey",
            "Recover",
            "Purge"
        ]
    }
}

resource "azurerm_key_vault_secret" "aks_backend_public_ssh_key" {
    name         = var.aks_backend_vm_public_ssh_secret_name
    value        = tls_private_key.aks_backend_ssh_key.public_key_openssh
    key_vault_id = azurerm_key_vault.aks_backend.id
}

resource "azurerm_key_vault_secret" "aks_backend_private_ssh_key" {
    name         = var.aks_backend_vm_private_ssh_secret_name
    value        = tls_private_key.aks_backend_ssh_key.private_key_pem
    key_vault_id = azurerm_key_vault.aks_backend.id
}


# +N IaC VM 
locals {
aks_backend_vm_custom_data = <<CUSTOM_DATA
#!/bin/bash
set -x
echo "Writing token to file" >> /var/log/script.log
apt-get update -y && apt-get upgrade -y  && apt-get dist-upgrade -y 
apt-get install git curl apt-transport-https ca-certificates software-properties-common -y
wget -O- https://apt.releases.hashicorp.com/gpg | gpg --dearmor | tee /usr/share/keyrings/hashicorp-archive-keyring.gpg 
gpg --no-default-keyring --keyring /usr/share/keyrings/hashicorp-archive-keyring.gpg --fingerprint 
echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | tee /etc/apt/sources.list.d/hashicorp.list
apt update -y && apt-get install terraform -y 
curl -sL https://aka.ms/InstallAzureCLIDeb -o installazcli.sh 
chmod +x installazcli.sh && ./installazcli.sh
az --version
apt-get install docker.io -y
curl -LO https://storage.googleapis.com/kubernetes-release/release/v1.21.0/bin/linux/amd64/kubectl
chmod +x ./kubectl
mv ./kubectl /usr/local/bin/kubectl
export PATH=$PATH:/usr/local/bin/kubectl
su azureuser -c 'cd ~; mkdir myagent && cd myagent && curl -LsS https://vstsagentpackage.azureedge.net/agent/3.220.0/vsts-agent-linux-x64-3.220.0.tar.gz -o vstsagent.tar.gz && tar -zxvf vstsagent.tar.gz'
su  azureuser -c 'cd ~/myagent && ./config.sh --unattended --url "${var.devops_url}" --auth pat --token "${var.personal_access_token_value}" --pool "${var.agent_pool}" --agent "${var.aks_agent_name}" --work _work --runAsService'
cd /home/azureuser/myagent && ./svc.sh install azureuser
cd /home/azureuser/myagent && ./svc.sh start
su azureuser -c 'cd ~/ && git clone https://$PERSONAL_ACCESS_TOKEN_VALUE@dev.azure.com/BiosenseWebsterIs/IoT%20DICOM%20Hub/_git/iotdicomhub'


# Trivy Scan
# Trivy 1
# cd iotdicomhub/src/agent/dotnet/ && 
# docker build -t test1:v1 -f Dockerfile .
# docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy image --exit-code 1 --severity HIGH,CRITICAL test1:v1

echo "Token written to file" >> /var/log/script.log
CUSTOM_DATA  
}


# +N IaC VM Network Interface Card
resource "azurerm_network_interface" "aks_backend_vm" {
    name                = var.aks_backend_vm_nic_name
    location            = data.azurerm_resource_group.aks_backend.location
    resource_group_name = data.azurerm_resource_group.aks_backend.name

    ip_configuration {
    name                          = "internal"
    subnet_id                     = data.azurerm_subnet.aks_backend.id
    private_ip_address_allocation = "Dynamic"
    }
}







resource "azurerm_linux_virtual_machine" "aks_backend_vm" {
    name                = var.aks_backend_vm_name
    location            = data.azurerm_resource_group.aks_backend.location
    resource_group_name = data.azurerm_resource_group.aks_backend.name
    network_interface_ids = [
        azurerm_network_interface.aks_backend_vm.id,
    ]
    size                = "Standard_B2s"
    computer_name  = var.aks_backend_vm_name
    admin_username = "azureuser"
    disable_password_authentication = true
    admin_ssh_key {
        username   = "azureuser"
        /* public_key = data.azurerm_key_vault_key.ssh_key.public_key_openssh */
        public_key = tls_private_key.aks_backend_ssh_key.public_key_openssh
        
        }

    source_image_reference {
        publisher = "Canonical"
        offer     = "UbuntuServer"
        sku       = "18.04-LTS"
        version   = "latest"
    }

    os_disk {
        caching              = "ReadWrite"
        storage_account_type = "Standard_LRS"
    }
    /* depends_on = [azurerm_marketplace_agreement.ubuntu] */
    /* custom_data = filebase64("${path.module}/iac-vm-init.sh") */
    custom_data = base64encode(local.aks_backend_vm_custom_data)
    /*  */
}