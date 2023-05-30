# Getting Started
## Microsoft Azure DICOM Service Setup - (This partThis part is documented for later automation)
### DICOM Service
1. Create new Resource group
2. Create new Health Data Services workspace:
* choose a subsciption and the Resource group
* give it a name and choose Region: West Europe
3. On the Resource group page of the Azure portal, select the name of your Azure Health Data Services workspace.
4. Select Deploy DICOM service.
5. Select Add DICOM service.
6. Enter a name for DICOM service, and then select Review + create.

### App Registration in Azure Active Directory
1. In the Azure portal, select Azure Active Directory.
2. Select App registrations.
3. Select New registration.
4. For Supported account types, select Accounts in this organization directory only. Leave the other options as is.
5. Select Register.
6. Select Certificates & Secrets and select New Client Secret.
7. Add and then copy the secret value.

### Assign roles for the DICOM service
1. In your DICOM Service select the **Access control (IAM)** blade. Select the **Role assignments** tab, and select + Add.
2.  Choose Roll **DICOM Data Owner**
3. Select your App Registration in the principal.

## Connect to DICOM Service drom C#

1. In a terminal run the following commands (from dotnet folder):
* DICOM Client package:
Add NuGet package source:

    `dotnet nuget add source https://microsofthealthoss.pkgs.visualstudio.com/FhirServer/_packaging/Public/nuget/v3/index.json`

    Then: `dotnet add package Microsoft.Health.Dicom.Client --version 10.0.282`
* fo-dicom package `dotnet add package fo-dicom --version 5.0.3`
* Azure.identity: `dotnet add package Azure.identity`