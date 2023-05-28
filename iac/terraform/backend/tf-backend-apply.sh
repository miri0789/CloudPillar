source ../.env

# terraform init -backend-config=../env/backend.tfvars
echo ""
echo "Azure Login With Service Principal"
echo ""

az login --service-principal -u $ARM_CLIENT_ID -p $ARM_CLIENT_SECRET --tenant $ARM_TENANT_ID
az account set --subscription $SUBSCRIPTION_ID

echo ""
echo "Terrform Backend Apply"
echo ""
terraform apply -var-file=../env/backend.tfvars -auto-approve


echo ""
echo "Finished TF Backend Deployment"
echo ""