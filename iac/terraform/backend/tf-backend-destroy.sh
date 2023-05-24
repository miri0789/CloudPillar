echo ""
echo "Destroying TF Backend Deployment"
echo ""
source ../.env
export ARM_CLIENT_ID=$ARM_CLIENT_ID
export ARM_CLIENT_SECRET=$ARM_CLIENT_SECRET
export ARM_TENANT_ID=$ARM_TENANT_ID
export ARM_SUBSCRIPTION_ID=$ARM_SUBSCRIPTION_ID

az login --service-principal -u $ARM_CLIENT_ID -p $ARM_CLIENT_SECRET --tenant $ARM_TENANT_ID
az account set --subscription $SUBSCRIPTION_ID
terraform init -var-file=../env/backend.tfvars
terraform destroy -var-file=../env/backend.tfvars -auto-approve
echo ""
echo "Finished TF Backend Destroy"
echo ""