echo ""
echo "Starting TF Backend Deployment"
echo ""
echo "Updating Variables in tf-backend.tfvars"
echo ""
source ../.env
sed -i "s/^tenant_id=.*/tenant_id=\"$ARM_TENANT_ID\"/" ../env/backend.tfvars
sed -i "s/^subscription_id=.*/subscription_id=\"$ARM_SUBSCRIPTION_ID\"/" ../env/backend.tfvars
sed -i "s/^subscription_id=.*/subscription_id=\"$ARM_SUBSCRIPTION_ID\"/" ../env/backend.tfvars
sed -i "s/^client_id=.*/client_id=\"$ARM_CLIENT_ID\"/" ../env/backend.tfvars
sed -i "s/^client_secret=.*/client_secret=\"$ARM_CLIENT_SECRET\"/" ../env/backend.tfvars
sed -i "s/^Location=.*/Location=\"$LOCATION\"/" ../env/backend.tfvars
sed -i "s/^region=.*/region=\"$REGION\"/" ../env/backend.tfvars
sed -i "s/^tf_backend_rg=.*/tf_backend_rg=\"$TF_BACKEND_RG\"/" ../env/backend.tfvars
sed -i "s/^tf_backend_sa=.*/tf_backend_sa=\"$TF_BACKEND_SA\"/" ../env/backend.tfvars
sed -i "s/^tf_backend_container=.*/tf_backend_container=\"$TF_BACKEND_CONTAINER\"/" ../env/backend.tfvars
sed -i "s/^tfstate_key=.*/tfstate_key=\"$TFSTATE_KEY\"/" ../env/backend.tfvars
sed -i "s/^tf_backend_kv=.*/tf_backend_kv=\"$TF_BACKEND_KEYVAULT\"/" ../env/backend.tfvars

# terraform init -backend-config=../env/backend.tfvars
echo ""
echo "Azure Login With Service Principal"
echo ""

az login --service-principal -u $ARM_CLIENT_ID -p $ARM_CLIENT_SECRET --tenant $ARM_TENANT_ID
az account set --subscription $SUBSCRIPTION_ID

echo ""
echo "Terrform Backend Init and Plan"
echo ""
terraform init -var-file=../env/backend.tfvars
terraform plan -var-file=../env/backend.tfvars

# read -p "Do you want to apply? - [Terraform Backend] - (Y/N): " apply_response
# if [[ $apply_response =~ ^[Yy]$ ]]
# then
#   terraform apply -var-file=../env/backend.tfvars -auto-approve
# fi

# echo ""
# echo "Finished TF Backend Deployment"
# echo ""