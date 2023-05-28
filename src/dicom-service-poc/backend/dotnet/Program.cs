using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace DicomBackendPoC
{
    partial class Program
    {  
        public static DicomServiceApi dicomServiceApi = new DicomServiceApi();
        private static async Task<bool> IsQueryTagEnabled(string tagPath)
        {
            var tagEntry = await dicomServiceApi.GetExtendedQueryTagAsync(tagPath);
            if (tagEntry.QueryStatus == Microsoft.Health.Dicom.Client.Models.QueryStatus.Enabled)
            {
                return true;
            }
            return false;
        }
  
        public static DicomDataset CreateRandomInstanceDataset(
        string? studyInstanceUid = null,
        string? seriesInstanceUid = null,
        string? sopInstanceUid = null,
        string? sopClassUid = null,
        DicomTransferSyntax? dicomTransferSyntax = null,
        string? patientId = null,
        bool validateItems = true)
        {
            var ds = new DicomDataset(dicomTransferSyntax ?? DicomTransferSyntax.ExplicitVRLittleEndian);

            if (!validateItems)
            {
                ds = ds.NotValidated();
            }

            ds.Add(DicomTag.StudyInstanceUID, studyInstanceUid ?? DicomUID.Generate().UID);
            ds.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid ?? DicomUID.Generate().UID);
            ds.Add(DicomTag.SOPInstanceUID, sopInstanceUid ?? DicomUID.Generate().UID);
            ds.Add(DicomTag.SOPClassUID, sopClassUid ?? DicomUID.Generate().UID);
            ds.Add(DicomTag.BitsAllocated, (ushort)8);
            ds.Add(DicomTag.PatientID, patientId ?? DicomUID.Generate().UID);
            ds.Add(DicomTag.PatientName, DicomUID.Generate().UID);
            return ds;
        }

        public static DicomFile CreateRandomDicomFile(
                string? studyInstanceUid = null,
                string? seriesInstanceUid = null,
                string? sopInstanceUid = null,
                bool validateItems = true)
        {
            return new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid, validateItems: validateItems));
        }

        private static async Task<DicomDataset> PostDicomFileAsync(DicomDataset? metadataItems = null)
        {
            DicomFile dicomFile = CreateRandomDicomFile();

            if (metadataItems != null)
            {
                dicomFile.Dataset.AddOrUpdate(metadataItems);
            }

            await dicomServiceApi.StoreDicom(dicomFile);
            return dicomFile.Dataset;
        }

        
        public static async Task QueryByModality_AllSeriesComputedColumns()
        {
            // 3 instances in the same study, 1 series with 2 instances in CT and 1 series with 1 instance in MR
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                { DicomTag.Modality, "CT" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            var seriesId = matchInstance.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            await PostDicomFileAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyId },
                { DicomTag.SeriesInstanceUID, seriesId },
                { DicomTag.Modality, "CT" }
            });
            await PostDicomFileAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyId },
                { DicomTag.Modality, "MR" }
            });
            var response = await dicomServiceApi.QuerySeriesAsync("Modality=CT&includefield=NumberOfSeriesRelatedInstances");

            DicomDataset[] datasets = await response.ToArrayAsync();
            DicomDataset? testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Console.WriteLine($"Number of series instances with Modality=CT: {testDataResponse?.GetSingleValue<int>(DicomTag.NumberOfSeriesRelatedInstances)}");
            
        }
        
        static async Task Main(string[] args)
        {
            string StudyInstanceUID = "12.3.6468.789454613000";
            string SeriesInstanceUID = "36541287984.3000";
            string[] SOPInstanceUIDs = {"1187945.3.6458.12000", "1187945.3.6458.12001", "1187945.3.6458.12002", "1187945.3.6458.12003"};

            // Store a Dicom File
            try
            {    
                var dicomFile = await DicomFile.OpenAsync("C:\\Dev\\dcm\\minimal_01.dcm");

                dicomFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, StudyInstanceUID);
                dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, SeriesInstanceUID);
                dicomFile.Dataset.AddOrUpdate(DicomTag.SOPInstanceUID, SOPInstanceUIDs[0]);

                var response = await dicomServiceApi.StoreDicom(dicomFile);

                Console.WriteLine("File storing status: " + response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while storing the dicom file: " + ex.Message);
            }
            
            // Retrieve the stored Dicom file and save locally
            try
            {
                var dicomFile = await dicomServiceApi.RetrieveInstanceAsync(StudyInstanceUID, SeriesInstanceUID, SOPInstanceUIDs[0]);
                dicomFile.Save("C:\\Dev\\dcm\\minimal_01_retrieved.dcm");
                Console.WriteLine("File saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while retrieving the dicom file: " + ex.Message);
            }
            
            // Retrieve the Dicom file dataset
            try
            {
                var dataset = await dicomServiceApi.RetrieveInstanceMetadataAsync(StudyInstanceUID, SeriesInstanceUID, SOPInstanceUIDs[0]);
                Console.WriteLine("Dicom file sopInstanceUID from dataset: " + dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while retrieving the dicom dataset: " + ex.Message);
            }

            // Store another instance in the same series
            try
            {
                var dicomFile = await DicomFile.OpenAsync("C:\\Dev\\dcm\\minimal_01_retrieved.dcm");
                dicomFile.Dataset.AddOrUpdate(DicomTag.SOPInstanceUID, SOPInstanceUIDs[1]);

                var response = await dicomServiceApi.StoreDicom(dicomFile);
                Console.WriteLine("File storing status: " + response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while storing the dicom file: " + ex.Message);
            }

            // Retrieve the Dicom files of the stored series
            try
            {
                var files = await dicomServiceApi.RetrieveSeriesDicomList(StudyInstanceUID, SeriesInstanceUID);
                Console.WriteLine($"Retrieved {files.Count} files of the requested series");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while retrieving the series dicom files list: " + ex.Message);
            }

            // Retrieve the Dicom files' datasets of the stored series
            try
            {
                var response = await dicomServiceApi.RetrieveSeriesMetadataAsync(StudyInstanceUID, SeriesInstanceUID);
                var datasets = await dicomServiceApi.GetDicomDatasetsListFromEnumerator(response);
                Console.WriteLine($"Retrieved {datasets.Count} datasets of the requested series. The sopInstanceUIDs are:");
                foreach(var dataset in datasets)
                {
                    Console.WriteLine(dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while retrieving the series datasets list: " + ex.Message);
            }

            // Retrieve all instances under a specific study
             try
            {
                var response = await dicomServiceApi.RetrieveStudyAsync(StudyInstanceUID);
                var files = await dicomServiceApi.GetDicomFilesListFromEnumerator(response);
                Console.WriteLine($"Retrieved {files.Count} Dicom files of the requested series. The sopInstanceUIDs are:");
                foreach(var file in files)
                {
                    Console.WriteLine(file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while retrieving the study instances list: " + ex.Message);
            }
            
            // Retrieve the Dicom files' datasets of a specific study
            try
            {
                var response = await dicomServiceApi.RetrieveStudyMetadataAsync(StudyInstanceUID);
                var datasets = await dicomServiceApi.GetDicomDatasetsListFromEnumerator(response);
                Console.WriteLine($"Retrieved {datasets.Count} datasets of the requested study. The sopInstanceUIDs are:");
                foreach(var dataset in datasets)
                {
                    Console.WriteLine(dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while retrieving the study datasets list: " + ex.Message);
            }

            await QueryByModality_AllSeriesComputedColumns();
            
            // Delete instance
            try
            {
                var filesBeforeDelete = await dicomServiceApi.RetrieveSeriesMetadataAsync(StudyInstanceUID, SeriesInstanceUID);
                Console.WriteLine("Before Delete:");
                await foreach(var dataset in filesBeforeDelete)
                {
                    Console.WriteLine(dataset.GetString(DicomTag.SOPInstanceUID));
                }
                await dicomServiceApi.DeleteInstanceAsync(StudyInstanceUID, SeriesInstanceUID, SOPInstanceUIDs[3]);
                var filesAfterDelete = await dicomServiceApi.RetrieveSeriesMetadataAsync(StudyInstanceUID, SeriesInstanceUID);
                Console.WriteLine("After Delete instance:");
                await foreach(var dataset in filesAfterDelete)
                {
                    Console.WriteLine(dataset.GetString(DicomTag.SOPInstanceUID));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            // Delete series
            try
            {
                await dicomServiceApi.DeleteSeriesAsync(StudyInstanceUID, SeriesInstanceUID);
                var filesAfterDelete = await dicomServiceApi.RetrieveStudyMetadataAsync(StudyInstanceUID);
                Console.WriteLine("After Delete series:");
                await foreach(var dataset in filesAfterDelete)
                {
                    Console.WriteLine(dataset.GetString(DicomTag.SOPInstanceUID));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

        }

    }

}
