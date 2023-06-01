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
* `dotnet add package Microsoft.Health.Dicom.Core -v 10.0.337`

## Private and standard tags

### Adding Private tag
To add private tag you need a Private Creator. The Private creator is automaticly added when create the new private tag with tag element (gggg,0010-00FF) (gggg is odd).
when you add new private tag you set the tag element with the low byte and it save with combine the low byte of the automatic Private Creator.


Example: 
```c#
Dataset ds = new Dataset();
var privateTag = new DicomTag(0x0009, 0x05, "BiosensePrivateGroup");
dicomFile.Dataset.AddOrUpdate(DicomVR.CS, ds.GetPrivateTag(privateTag), "value");
```

This code generate:
1. One tag of Private Creator:
**(0009, 0010)** with VR: **LO** and value: **BiosensePrivateGroup**.
2. Another tag with combine element: **(0009, 1005)** with VR: **CS** and value: **value**.

**Important!** 

Before save/store the DICOM with the private element be sure the Transfare syntax of the Dicom is ExplicitVRLittleEndian.
This is update the Transfare syntax: `dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;`

### Query with Tag filter
The query is divided into three types:
#### *Supported Tags*
There are list of tag that the Microsoft Azure Dicom Service support to qury by use them. they listed here: 
https://learn.microsoft.com/en-us/azure/healthcare-apis/dicom/dicom-services-conformance-statement#searchable-attributes.
#### *Standard Tags*
We can extend the Query tags by using `QueryDicomExpandedStandardTag()`.

example for query all the instances with tag PatientSex==F:
```c#
DicomTag genderTag = DicomTag.PatientSex;
QueryDicomExpandedStandardTag(genderTag, "F");
```

#### *Private Tags*
We can extend the Query tags by using `QueryDicomExpandedPrivateTag()`.

example for query all the instances with tag (0x0009, 0x1005)==value:
```c#
var privateTag = new DicomTag(0x0009, 0x05, "BiosensePrivateGroup");
QueryDicomExpandedPrivateTag(privateTag, DicomVR.CS, QueryTagLevel.Study "value");
```
