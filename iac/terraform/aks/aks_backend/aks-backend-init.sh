echo ""
echo "Starting AKS Backend Deployment"
echo ""
echo "Updating Variables in aks-backend.tfvars"
echo ""

source ../.env
sed -i "s/^tenant_id=.*/tenant_id=\"$ARM_TENANT_ID\"/" ../env/aks-backend.tfvars
sed -i "s/^subscription_id=.*/subscription_id=\"$ARM_SUBSCRIPTION_ID\"/" ../env/aks-backend.tfvars
sed -i "s/^client_id=.*/client_id=\"$ARM_CLIENT_ID\"/" ../env/aks-backend.tfvars
sed -i "s/^client_secret=.*/client_secret=\"$ARM_CLIENT_SECRET\"/" ../env/aks-backend.tfvars
sed -i "s/^Location=.*/Location=\"$LOCATION\"/" ../env/aks-backend.tfvars
sed -i "s/^region=.*/region=\"$REGION\"/" ../env/aks-backend.tfvars
sed -i "s/^tf_backend_rg=.*/tf_backend_rg=\"$TF_BACKEND_RG\"/" ../env/aks-backend.tfvars
sed -i "s/^tf_backend_sa=.*/tf_backend_sa=\"$TF_BACKEND_SA\"/" ../env/aks-backend.tfvars
sed -i "s/^tf_backend_container=.*/tf_backend_container=\"$TF_BACKEND_CONTAINER\"/" ../env/aks-backend.tfvars
sed -i "s/^tfstate_key=.*/tfstate_key=\"$TFSTATE_KEY\"/" ../env/aks-backend.tfvars
sed -i "s/^tf_backend_kv=.*/tf_backend_kv=\"$TF_BACKEND_KEYVAULT\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_subnet=.*/aks_subnet=\"$AKS_SUBNET\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_vnet=.*/aks_vnet=\"$AKS_VNET\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_rg=.*/aks_rg=\"$AKS_RG\"/" ../env/aks-backend.tfvars
# sed -i "s/^devops_url=.*/devops_url=\"$DEVOPS_URL\"/" ../env/aks-backend.tfvars
sed -i 's#^devops_url=.*#devops_url="'"$DEVOPS_URL"'"#' ../env/aks-backend.tfvars
sed -i "s/^agent_pool=.*/agent_pool=\"$AGENT_POOL\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_agent_name=.*/aks_agent_name=\"$AKS_AGENT_NAME\"/" ../env/aks-backend.tfvars
sed -i "s/^personal_access_token_secret=.*/personal_access_token_secret=\"$PERONAL_ACCESS_TOKEN_SECRET\"/" ../env/aks-backend.tfvars
sed -i "s/^personal_access_token_value=.*/personal_access_token_value=\"$PERSONAL_ACCESS_TOKEN_VALUE\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_backend_rg=.*/aks_backend_rg=\"$AKS_BACKEND_RG\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_backend_kv=.*/aks_backend_kv=\"$AKS_BACKEND_KV\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_ssh_user=.*/aks_ssh_user=\"$AKS_SSH_USER\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_backend_vm_public_ssh_secret_name=.*/aks_backend_vm_public_ssh_secret_name=\"$AKS_PUBLIC_SSH_SECRET_NAME\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_backend_vm_private_ssh_secret_name=.*/aks_backend_vm_private_ssh_secret_name=\"$AKS_PRIVATE_SSH_SECRET_NAME\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_backend_vm_nic_name=.*/aks_backend_vm_nic_name=\"$AKS_BACKEND_VM_NIC_NAME\"/" ../env/aks-backend.tfvars
sed -i "s/^aks_backend_vm_name=.*/aks_backend_vm_name=\"$AKS_BACKEND_VM_NAME\"/" ../env/aks-backend.tfvars




echo ""
echo "Azure Login With Service Principal"
echo ""

export ARM_CLIENT_ID=$ARM_CLIENT_ID
export ARM_CLIENT_SECRET=$ARM_CLIENT_SECRET
export ARM_TENANT_ID=$ARM_TENANT_ID
export ARM_SUBSCRIPTION_ID=$ARM_SUBSCRIPTION_ID

az login --service-principal -u $ARM_CLIENT_ID -p $ARM_CLIENT_SECRET --tenant $ARM_TENANT_ID
az account set --subscription $ARM_SUBSCRIPTION_ID

echo ""
echo "AKS Terrform Init and Plan"
echo ""
terraform init -var-file=../env/aks-backend.tfvars \
  -backend-config="subscription_id=$ARM_SUBSCRIPTION_ID" \
  -backend-config="resource_group_name=$TF_BACKEND_RG" \
  -backend-config="storage_account_name=$TF_BACKEND_SA" \
  -backend-config="container_name=$TF_BACKEND_CONTAINER" \
  -backend-config="key=$AKS_BACKEND_TFSTATE_KEY"

terraform plan -var-file=../env/aks-backend.tfvars

read -p "Do you want to apply - [AKS Backend] - ? (Y/N): " apply_response
if [[ $apply_response =~ ^[Yy]$ ]]
then
  terraform apply -var-file=../env/aks-backend.tfvars -auto-approve
fi


echo ""
echo "Finished AKS Backend Deployment"
echo ""