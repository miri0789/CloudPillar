using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;

namespace DicomBackendPoC
{

    partial class Program
    {  
        static async Task Main(string[] args)
        {
            DicomServiceApi dicomServiceApi = new DicomServiceApi();
            string StudyInstanceUID = "12.3.6468.789454613000";
            string SeriesInstanceUID = "36541287984.3000";
            string[] SOPInstanceUIDs = {"1187945.3.6458.12000", "1187945.3.6458.12001"};

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

        }

    }

}
