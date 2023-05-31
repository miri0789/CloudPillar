using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;

namespace DicomAgentPoC
{
    public class DicomManagerTest
    {
        public DicomManager dicomManager = new DicomManager();
        public string dicomFilePath = @"C:\Dev\minimal_01.dcm";
        public string zipFilePath = @"C:\Dev\Catheter-501.zip";

        public DicomManagerTest()
        {
            dicomManager.DicomServiceConnection();
        }
        
        /// <summary>
        /// Store Dicom with binary tag in Dicom Service.
        /// </summary>
        public async Task StoreDicomWithBinaryTag()
        {
            // Open a new DICOM dataset and add the binary data element to it
            var dicomFile = await DicomFile.OpenAsync(dicomFilePath);
            var binaryData = File.ReadAllBytes(zipFilePath);

            var sizeOfZipDicomTag = new DicomTag(0x0009, 0x03, "BiosensePrivateGroup");
            var zipDicomTag = new DicomTag(0x0009, 0x05, "BiosensePrivateGroup");

            // Create a new binary data element for the private tag
            var dicomWithZipTag = dicomManager.ExpandDicomWithNewPrivateTag(dicomFile, zipDicomTag, DicomVR.OB, binaryData);
            var updetedDicom = dicomManager.ExpandDicomWithNewPrivateTag(dicomWithZipTag, sizeOfZipDicomTag, DicomVR.US, Convert.ToUInt16(binaryData.Length));
            
            var xx = dicomManager.SetDicomStandardTag(updetedDicom, DicomTag.SOPInstanceUID, DicomUID.Generate().UID);
            var response = await dicomManager.StoreDicom(xx);
            if(response.IsSuccessStatusCode)
                Console.WriteLine($"{updetedDicom.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)} saved with status code: {response.StatusCode}");
        }

        /// <summary>
        /// Query Dicom with expended standard tag.
        /// </summary>
        public async Task QueryDicomWithExpandedStandardTag()
        {
            var dicomFile = await DicomFile.OpenAsync(dicomFilePath);
            var binaryData = File.ReadAllBytes(zipFilePath);

            DicomTag genderTag = DicomTag.PatientSex;

            var updetedDicom = dicomManager.SetDicomStandardTag(dicomFile, DicomTag.SOPInstanceUID, DicomUID.Generate().UID);          
            updetedDicom = dicomManager.SetDicomStandardTag(updetedDicom, genderTag,"F");          
            var response = await dicomManager.StoreDicom(updetedDicom);
            if(!response.IsSuccessStatusCode)
                Console.WriteLine($"{dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)} saved with status code: {response.StatusCode}");
            else
            {
                var instance = await dicomManager.QueryDicomExpandedStandardTag(genderTag, "F");
                foreach (var item in instance)
                {
                    Console.WriteLine($"{item.GetSingleValue<string>(DicomTag.SOPInstanceUID)} queried with tag: {genderTag.GetPath()} value is: {item.GetSingleValue<string>(genderTag)}");
                }
            }
        }

        /// <summary>
        /// Query Dicom with expended standard tag.
        /// </summary>
        public async Task QueryDicomWithExpandedPrivateTag()
        {
            DicomDataset ds = new DicomDataset();
            var dicomFile = await DicomFile.OpenAsync(dicomFilePath);
            var binaryData = File.ReadAllBytes(zipFilePath);

            var sizeOfZipDicomTag = new DicomTag(0x0009, 0x08, "BiosensePrivateGroup");
            var updetedDicom = dicomManager.ExpandDicomWithNewPrivateTag(dicomFile, sizeOfZipDicomTag, DicomVR.US, Convert.ToUInt16(binaryData.Length));
            dicomManager.SetDicomStandardTag(updetedDicom, DicomTag.SOPInstanceUID, DicomUID.Generate().UID);
            // var response = await dicomManager.StoreDicom(updetedDicom);
            // if(response.IsSuccessStatusCode)
                // Console.WriteLine($"{updetedDicom.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)} saved with status code: {response.StatusCode}");

            var instance = await dicomManager.QueryDicomExpandedPrivateTag(sizeOfZipDicomTag, DicomVR.CS, QueryTagLevel.Instance, "aaa");
            foreach (var item in instance)
            {
                Console.WriteLine($"{item.GetSingleValue<string>(DicomTag.SOPInstanceUID)} queried with tag: {ds.GetPrivateTag(sizeOfZipDicomTag).GetPath()}");
            }
        }
    }
}