## Prerequisites

1. Infrastructure Prerequisites for each environment:
    1. Service Principal Creation
        1. Client ID
        2. Client Secret
    2. Azure DevOps Agent Pool
    3. Azure DevOps Personal Access Token

2. Backend Infrastructure Prerequisites for each environment:
    1. Azure Storage Account
    2. Azure Storage Container
    3. Azure Key Vault
    4. Azure Key Vault Secrets
        1. Azure Client ID
        2. Azure Client Secret
        3. Azure DevOps Personal Access Token

3. Local Exectuion:
        1. Creating Environment Variables Files
            1. All.env - contains all general environments variables
                1. ARM_TENANT_ID
                2. LOCATION
                3. REGION
                4. DEVOPS_URL
                5. AGENT_POOL
                6. PAT_SECRET_NAME
                7. ARM_SUBSCRIPTION_NAME
                    1. Cloud Pillar <env>
                8. ARM_CLIENT_ID
                    1. NAME - cp-tf-client-id-<env>
                9. ARM_CLIENT_SECRET
                    1. NAME - cp-tf-client-secret-<env> 
                10. TF_BACKEND_RG
                    1. cp-tf-backend-rg-<env>
                11. TF_BACKEND_SA
                    1. cptfbackendsa<env>
                12. TF_BACKEND_CONTAINER
                    1. tfstate
                13. TF_BACKEND_KV
                    1. cp-tf-backend-kv-<env>
                14. <env>_ARM_SUBSCRIPTION_ID
                    1. <subscription_id>      
    2. Software & Hardware Requirements
        1. Terraform
        2. Azure CLI
        3. Git
        4. Docker
        5. Kubernetes
        6. Kubectl
        7. Azure DevOps Agent Installation
        8. Azure DevOps Agent Configuration

4. Pipeline Execution:
    1. Azure DevOps Pipeline
        1. Parameters:
            1. env - <env>
            2. Action - <plan/apply/destroy>

        2. Pipeline Tasks
            1. Terraform Init
            2. Terraform Plan
            3. Terraform Apply
            4. Terraform Destroy

    2. Variable Groups
        1. Variable Group: arm-vg
            1. AGENT_POOL
            2. ARM_TENANT_ID
            3. DEVOPS_URL
            4. LOCATION
            5. REGION
            6. PAT_SECRET_NAME

        2. Variable Group: <env>-vg
            1. ARM_CLIENT_ID
            2. ARM_CLIENT_SECRET
            3. ARM_SUBSCRIPTION_ID
            4. ARM_SUBSCRIPTION_NAME
                1. Cloud Pillar <env>
            5. ENV
                1. <env>
            6. ACR_NAME
                1. cp<env>acr

        3. Variable Group: iac-backend-vg
            1. TF_BACKEND_RG
                1. cp-tf-backend-rg-<env>
            2. TF_BACKEND_SA
                1. cptfbackendsa<env>
            3. TF_BACKEND_CONTAINER
                1. tfstate
            4. TF_BACKEND_KV
                1. cp-tf-backend-kv-<env>

        4. Variable Group: backend-secrets-vg - Connected to Key Vault
            1. ARM_CLIENT_ID
                1. NAME - cp-tf-client-id-<env>
            2. ARM_CLIENT_SECRET
                1. NAME - cp-tf-client-secret-<env> 
            3. PAT
                1. NAME - cp-tf-backend-pat

    3. Software & Hardware Requirements
        1. Terraform
        2. Azure CLI
        3. Git
        4. Docker
        5. Kubernetes
        6. Kubectl
        7. Azure DevOps Agent Installation
        8. Azure DevOps Agent Configuration

5. Permission & Credentials:
    1. Azure DevOps Service Connection
    2. Azure DevOps Backend Secrets Vault - Variable Groups