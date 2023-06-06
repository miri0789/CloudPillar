#* Terraform Provider Configurations
data "azurerm_client_config" "current" {}
data "azuread_client_config" "current" {}



#* Terraform Import Resources
data "azurerm_resource_group" "aks_backend" {
    name = "iot-${var.env}-rg"
}

data "azurerm_subnet" "aks_backend" {
    name                 = "aks-subnet"
    virtual_network_name = "iot-${var.env}-vnet"
    resource_group_name  = "iot-${var.env}-rg"
}

data "azurerm_virtual_network" "aks_backend" {
    name                 = "iot-${var.env}-vnet"
    resource_group_name  = "iot-${var.env}-rg"
}



locals {
iac_backend_vm_custom_data = <<CUSTOM_DATA
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
su azureuser -c 'cd ~; mkdir myagent && cd myagent && curl -LsS https://vstsagentpackage.azureedge.net/agent/3.220.0/vsts-agent-linux-x64-3.220.0.tar.gz -o vstsagent.tar.gz && tar -zxvf vstsagent.tar.gz'
su  azureuser -c 'cd ~/myagent && ./config.sh --unattended --url "${var.devops_url}" --auth pat --token "${var.personal_access_token_value}" --pool "${var.agent_pool}" --agent "iac-${var.env}-agent" --work _work --runAsService'
cd /home/azureuser/myagent && ./svc.sh install azureuser
cd /home/azureuser/myagent && ./svc.sh start
su azureuser -c 'cd ~/ && git clone https://${var.personal_access_token_value}@dev.azure.com/BiosenseWebsterIs/CloudPillar/_git/CloudPillar'

echo "Token written to file" >> /var/log/script.log
CUSTOM_DATA  

    tags = {
        environment = var.env
        terraform = true
        creator = "GM"
        relation = "iac_backend"
        phase = "backend"        
    }

}


resource "azurerm_resource_group" "iac_backend" {
    name     = "iac-${var.env}-backend-rg"
    location = var.location
    tags = local.tags

}


resource "tls_private_key" "iac_backend_ssh_key" {
    algorithm = "RSA"
    rsa_bits = 4096
}







#* Key Vaults

# +N IaC Key Vault
resource "azurerm_key_vault" "iac_backend" { 
    name = "iac-${var.env}-backend-kv"
    location = azurerm_resource_group.iac_backend.location
    resource_group_name = azurerm_resource_group.iac_backend.name
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
    tags = local.tags

}

resource "azurerm_key_vault_secret" "iac_backend_public_ssh_key" {
    name         = "iac-${var.env}-backend-ssh-pub"
    value        = tls_private_key.iac_backend_ssh_key.public_key_openssh
    key_vault_id = azurerm_key_vault.iac_backend.id
    tags = local.tags

}

resource "azurerm_key_vault_secret" "aks_backend_private_ssh_key" {
    name         = "iac-${var.env}-backend-ssh-private"
    value        = tls_private_key.iac_backend_ssh_key.private_key_pem
    key_vault_id = azurerm_key_vault.iac_backend.id
    tags = local.tags

}








# +N IaC VM Network Interface Card
resource "azurerm_network_interface" "iac_backend_vm" {
    name                = "iac-${var.env}-backend-nic"
    location            = data.azurerm_resource_group.aks_backend.location
    resource_group_name = data.azurerm_resource_group.aks_backend.name

    ip_configuration {
    name                          = "internal"
    subnet_id                     = data.azurerm_subnet.aks_backend.id
    private_ip_address_allocation = "Dynamic"
    }
    tags = local.tags

}







resource "azurerm_linux_virtual_machine" "iac_backend_vm" {
    name                = "iac-${var.env}-backend-vm"
    location            = data.azurerm_resource_group.aks_backend.location
    resource_group_name = data.azurerm_resource_group.aks_backend.name
    network_interface_ids = [
        azurerm_network_interface.iac_backend_vm.id,
    ]
    size                = "Standard_B2s"
    computer_name  = "iac-${var.env}-backend-vm"
    admin_username = "azureuser"
    disable_password_authentication = true
    admin_ssh_key {
        username   = "azureuser"
        /* public_key = data.azurerm_key_vault_key.ssh_key.public_key_openssh */
        public_key = tls_private_key.iac_backend_ssh_key.public_key_openssh
        
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
    custom_data = base64encode(local.iac_backend_vm_custom_data)
    /*  */
    tags = local.tags

}