# az authentication commands
az login
az account show
az account list
For each environment:
    az account set --subscription <id>

# terraform commands
terraform init
For each environment:
    terraform workspace new <env>
    terraform plan -var-file "<env>.tfvars" -out tfplan
    terraform apply tfplan
