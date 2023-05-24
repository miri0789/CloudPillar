echo ""
echo "Starting IaC Backend Deployment"
echo ""
echo "Updating Variables in iac-backend.tfvars"
echo ""

source ../.env
sed -i "s/^tenant_id=.*/tenant_id=\"$ARM_TENANT_ID\"/" ../env/iac-backend.tfvars
sed -i "s/^subscription_id=.*/subscription_id=\"$ARM_SUBSCRIPTION_ID\"/" ../env/iac-backend.tfvars
sed -i "s/^client_id=.*/client_id=\"$ARM_CLIENT_ID\"/" ../env/iac-backend.tfvars
sed -i "s/^client_secret=.*/client_secret=\"$ARM_CLIENT_SECRET\"/" ../env/iac-backend.tfvars
sed -i "s/^Location=.*/Location=\"$LOCATION\"/" ../env/iac-backend.tfvars
sed -i "s/^region=.*/region=\"$REGION\"/" ../env/iac-backend.tfvars
sed -i "s/^tf_backend_rg=.*/tf_backend_rg=\"$TF_BACKEND_RG\"/" ../env/iac-backend.tfvars
sed -i "s/^tf_backend_sa=.*/tf_backend_sa=\"$TF_BACKEND_SA\"/" ../env/iac-backend.tfvars
sed -i "s/^tf_backend_container=.*/tf_backend_container=\"$TF_BACKEND_CONTAINER\"/" ../env/iac-backend.tfvars
sed -i "s/^tfstate_key=.*/tfstate_key=\"$TFSTATE_KEY\"/" ../env/iac-backend.tfvars
sed -i "s/^tf_backend_kv=.*/tf_backend_kv=\"$TF_BACKEND_KEYVAULT\"/" ../env/iac-backend.tfvars
sed -i "s/^aks_subnet=.*/aks_subnet=\"$AKS_SUBNET\"/" ../env/iac-backend.tfvars
sed -i "s/^aks_vnet=.*/aks_vnet=\"$AKS_VNET\"/" ../env/iac-backend.tfvars
sed -i "s/^aks_rg=.*/aks_rg=\"$AKS_RG\"/" ../env/iac-backend.tfvars
# sed -i "s/^devops_url=.*/devops_url=\"$DEVOPS_URL\"/" ../env/iac-backend.tfvars
sed -i 's#^devops_url=.*#devops_url="'"$DEVOPS_URL"'"#' ../env/iac-backend.tfvars
sed -i "s/^agent_pool=.*/agent_pool=\"$AGENT_POOL\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_agent_name=.*/iac_agent_name=\"$IAC_AGENT_NAME\"/" ../env/iac-backend.tfvars
sed -i "s/^personal_access_token_secret=.*/personal_access_token_secret=\"$PERONAL_ACCESS_TOKEN_SECRET\"/" ../env/iac-backend.tfvars
sed -i "s/^personal_access_token_value=.*/personal_access_token_value=\"$PERSONAL_ACCESS_TOKEN_VALUE\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_backend_rg=.*/iac_backend_rg=\"$IAC_BACKEND_RG\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_backend_kv=.*/iac_backend_kv=\"$IAC_BACKEND_KV\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_ssh_user=.*/iac_ssh_user=\"$IAC_SSH_USER\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_public_ssh_secret_name=.*/iac_public_ssh_secret_name=\"$IAC_PUBLIC_SSH_SECRET_NAME\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_private_ssh_secret_name=.*/iac_private_ssh_secret_name=\"$IAC_PRIVATE_SSH_SECRET_NAME\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_backend_vm_nic_name=.*/iac_backend_vm_nic_name=\"$IAC_BACKEND_VM_NIC_NAME\"/" ../env/iac-backend.tfvars
sed -i "s/^iac_backend_vm_name=.*/iac_backend_vm_name=\"$IAC_BACKEND_VM_NAME\"/" ../env/iac-backend.tfvars


echo ""
echo "Azure Login With Service Principal"
echo ""

export ARM_CLIENT_ID=$ARM_CLIENT_ID
export ARM_CLIENT_SECRET=$ARM_CLIENT_SECRET
export ARM_TENANT_ID=$ARM_TENANT_ID
export ARM_SUBSCRIPTION_ID=$ARM_SUBSCRIPTION_ID
az login --service-principal -u $ARM_CLIENT_ID -p $ARM_CLIENT_SECRET --tenant $ARM_TENANT_ID
az account set --subscription $SUBSCRIPTION_ID

echo ""
echo "IaC Terrform Init and Plan"
echo ""
terraform init -var-file=../env/iac-backend.tfvars \
  -backend-config="subscription_id=$SUBSCRIPTION_ID" \
  -backend-config="resource_group_name=$TF_BACKEND_RG" \
  -backend-config="storage_account_name=$TF_BACKEND_SA" \
  -backend-config="container_name=$TF_BACKEND_CONTAINER" \
  -backend-config="key=$IAC_BACKEND_TFSTATE_KEY"
terraform plan -var-file=../env/iac-backend.tfvars

read -p "Do you want to apply - [IaC Backend] - ? (Y/N): " apply_response
if [[ $apply_response =~ ^[Yy]$ ]]
then
  terraform apply -var-file=../env/iac-backend.tfvars -auto-approve
fi


echo ""
echo "Finished IaC Backend Deployment"
echo ""