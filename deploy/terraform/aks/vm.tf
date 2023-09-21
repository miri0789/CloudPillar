
locals {
  devopsURL = "https://dev.azure.com/BiosenseWebsterIs"
  agentPool = "Cloud-Pillar-Pool"
  aks_vm_custom_data = <<CUSTOM_DATA
#!/bin/bash
set -x
#!/bin/bash
# This is a script to install updates and upgrades to the system
echo "Writing token to file" >> /var/log/script.log
apt-get install git curl apt-transport-https ca-certificates software-properties-common -y
wget -O- https://apt.releases.hashicorp.com/gpg | gpg --dearmor | tee /usr/share/keyrings/hashicorp-archive-keyring.gpg
gpg --no-default-keyring --keyring /usr/share/keyrings/hashicorp-archive-keyring.gpg --fingerprint 
echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | tee /etc/apt/sources.list.d/hashicorp.list
apt update -y && apt-get install terraform -y 
apt-get update -y && apt-get upgrade -y  && apt-get dist-upgrade -y
curl -sL https://aka.ms/InstallAzureCLIDeb -o installazcli.sh
chmod +x installazcli.sh && ./installazcli.sh
az --version
# Install Helm
curl -fsSL -o get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3
chmod 700 get_helm.sh
./get_helm.sh
# Install Docker
apt-get install docker.io -y
curl -LO https://storage.googleapis.com/kubernetes-release/release/v1.21.0/bin/linux/amd64/kubectl
chmod +x ./kubectl
mv ./kubectl /usr/local/bin/kubectl
export PATH=$PATH:/usr/local/bin/kubectl
su azureuser -c 'cd ~; mkdir myagent && cd myagent && curl -LsS https://vstsagentpackage.azureedge.net/agent/3.220.0/vsts-agent-linux-x64-3.220.0.tar.gz -o vstsagent.tar.gz && tar -zxvf vstsagent.tar.gz'

su azureuser -c 'cd ~/myagent && ./config.sh --unattended --url "${local.devopsURL}" --auth pat --token "${azurerm_key_vault_secret.kv_pat.value}" --pool "${local.agentPool}" --agent "aks-${var.env}-vm-agent" --work _work --runAsService'
cd /home/azureuser/myagent && ./svc.sh install azureuser
cd /home/azureuser/myagent && ./svc.sh start
su azureuser -c 'cd ~/ && git clone https://${azurerm_key_vault_secret.kv_pat.value}@dev.azure.com/BiosenseWebsterIs/CloudPillar/_git/CloudPillar'
curl -fsSL -o get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3
chmod 700 get_helm.sh
echo "Token written to file" >> /var/log/script.log
  CUSTOM_DATA
}

resource "tls_private_key" "aks_vm_ssh_key" {
  algorithm = "RSA"
  rsa_bits = 4096
}

resource "azurerm_key_vault_secret" "aks_vm_private_ssh_key" {
  name         = "AksVmSshPrivate"
  value        = tls_private_key.aks_vm_ssh_key.private_key_pem
  key_vault_id = azurerm_key_vault.infr.id
  tags         = { Terraform = true }
}

resource "azurerm_network_interface" "aks_vm" {
  name                = "aks-${var.env}-nic"
  location            = azurerm_resource_group.aks.location
  resource_group_name = azurerm_resource_group.aks.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.aks.id
    private_ip_address_allocation = "Dynamic"
  }
  tags = { Terraform = true }
}

resource "azurerm_linux_virtual_machine" "aks_vm" {
  name                = "aks-${var.env}-vm"
  location            = azurerm_resource_group.aks.location
  resource_group_name = azurerm_resource_group.aks.name
  network_interface_ids = [
    azurerm_network_interface.aks_vm.id,
  ]
  size                = "Standard_B2s"
  computer_name  = "aks-${var.env}-vm"
  admin_username = "azureuser"
  disable_password_authentication = true
  admin_ssh_key {
    username   = "azureuser"
    public_key = tls_private_key.aks_vm_ssh_key.public_key_openssh
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
  custom_data = base64encode(local.aks_vm_custom_data)
  identity  { type = "SystemAssigned" }
  tags =    { Terraform = true }
  lifecycle { ignore_changes = [ custom_data ] }
}

resource "azurerm_dev_test_global_vm_shutdown_schedule" "aks_vm" {
  virtual_machine_id = azurerm_linux_virtual_machine.aks_vm.id
  location           = azurerm_resource_group.aks.location
  enabled            = true
  daily_recurrence_time = "1800"
  timezone              = "Israel Standard Time"
  notification_settings { enabled = false }
}