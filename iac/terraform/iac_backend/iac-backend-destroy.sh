echo ""
echo "Destroying IaC Backend Deployment"
echo ""
source ../.env
export ARM_CLIENT_ID=$ARM_CLIENT_ID
export ARM_CLIENT_SECRET=$ARM_CLIENT_SECRET
export ARM_TENANT_ID=$ARM_TENANT_ID
export ARM_SUBSCRIPTION_ID=$ARM_SUBSCRIPTION_ID



az login --service-principal -u $ARM_CLIENT_ID -p $ARM_CLIENT_SECRET --tenant $ARM_TENANT_ID
az account set --subscription $ARM_SUBSCRIPTION_ID

# terraform init -var-file=../env/iac-backend.tfvars \
#   -backend-config="subscription_id=$SUBSCRIPTION_ID" \
#   -backend-config="resource_group_name=$TF_BACKEND_RG" \
#   -backend-config="storage_account_name=$TF_BACKEND_SA" \
#   -backend-config="container_name=$TF_BACKEND_CONTAINER" \
#   -backend-config="key=$IAC_BACKEND_TFSTATE_KEY"
./iac-backend-init.sh
terraform destroy -var-file=../env/iac-backend.tfvars -auto-approve
echo ""
echo "Finished IaC Backend Destroy"
echo ""






