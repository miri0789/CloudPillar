using FellowOakDicom;

namespace DicomBackendPoC
{
    public class DicomServiceApiTest
    {
        public DicomServiceApi dicomServiceApi = new DicomServiceApi();

        private string StudyInstanceUID = "12.3.6468.789454613000";
        private string SeriesInstanceUID = "36541287984.3000";
        private string[] SOPInstanceUIDs = {"1187945.3.6458.12000", "1187945.3.6458.12001", "1187945.3.6458.12002", "1187945.3.6458.12003"};

        public DicomServiceApiTest() {}

        private async Task<bool> IsQueryTagEnabled(string tagPath)
        {
            var tagEntry = await dicomServiceApi.GetExtendedQueryTagAsync(tagPath);
            if (tagEntry.QueryStatus == Microsoft.Health.Dicom.Client.Models.QueryStatus.Enabled)
            {
                return true;
            }
            return false;
        }

        public DicomDataset CreateRandomInstanceDataset(
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

            public DicomFile CreateRandomDicomFile(
                    string? studyInstanceUid = null,
                    string? seriesInstanceUid = null,
                    string? sopInstanceUid = null,
                    bool validateItems = true)
            {
                return new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid, validateItems: validateItems));
            }

            private async Task<DicomDataset> PostDicomFileAsync(DicomDataset? metadataItems = null)
            {
                DicomFile dicomFile = CreateRandomDicomFile();

                if (metadataItems != null)
                {
                    dicomFile.Dataset.AddOrUpdate(metadataItems);
                }

                await dicomServiceApi.StoreDicom(dicomFile);
                return dicomFile.Dataset;
            }

            
            public async Task QueryByModality()
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

            public async Task DeleteAllStudies()
            {
                var datasets = await dicomServiceApi.QueryStudyAsync("");
                await foreach (var ds in datasets)
                {
                    await dicomServiceApi.DeleteStudyAsync(ds.GetString(DicomTag.StudyInstanceUID));
                }
            }

            public async Task StoreAndRetrieveDicomFile()
            {
                // First clean 
                await DeleteAllStudies();

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
            }

            public async Task ValidateDicomDatasets()
            {
                await DeleteAllStudies();

                int count = 0;

                // Store a random DICOM files
                DicomDataset ds1 = await PostDicomFileAsync();
                count++;

                var studyUID = ds1.GetString(DicomTag.StudyInstanceUID);
                var seriesUID = ds1.GetString(DicomTag.SeriesInstanceUID);
                var instanceUID = ds1.GetString(DicomTag.SOPInstanceUID);

                Console.WriteLine($"Dicom file stored sopInstanceUID: {instanceUID}");
                
                // Retrieve the Dicom file dataset from cloud
                var dataset = await dicomServiceApi.RetrieveInstanceMetadataAsync(studyUID, seriesUID, instanceUID);
                Console.WriteLine($"Dicom file sopInstanceUID from dataset: {dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)}");

                // Store another instance in the same series
                await PostDicomFileAsync(new DicomDataset()
                {
                    { DicomTag.StudyInstanceUID, studyUID },
                    { DicomTag.SeriesInstanceUID, seriesUID }
                });
                count++;

                // Retrieve the Dicom files of the stored series and check their count
                var files = await dicomServiceApi.RetrieveSeriesDicomList(studyUID, seriesUID);
                Console.WriteLine($"Stored {count} files, retrieved {files.Count} files of series: {seriesUID}");

                // Retrieve all instances under a specific study
                var response = await dicomServiceApi.RetrieveStudyAsync(studyUID);
                var fileList = await dicomServiceApi.GetDicomFilesListFromEnumerator(response);
                Console.WriteLine($"Retrieved {fileList.Count} Dicom files of the requested series. The sopInstanceUIDs are:");
                foreach(var file in fileList)
                {
                    Console.WriteLine(file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                }
            }

            public async Task DeleteInstanceTest()
            {
                await DeleteAllStudies();

                // Store 2 random DICOM files
                await PostDicomFileAsync(new DicomDataset()
                {
                    { DicomTag.StudyInstanceUID, StudyInstanceUID },
                    { DicomTag.SeriesInstanceUID, SeriesInstanceUID },
                    { DicomTag.SOPInstanceUID, SOPInstanceUIDs[2] }
                });
                await PostDicomFileAsync(new DicomDataset()
                {
                    { DicomTag.StudyInstanceUID, StudyInstanceUID },
                    { DicomTag.SeriesInstanceUID, SeriesInstanceUID },
                    { DicomTag.SOPInstanceUID, SOPInstanceUIDs[3] }
                });

                // List the stored files
                var filesBeforeDelete = await dicomServiceApi.RetrieveSeriesMetadataAsync(StudyInstanceUID, SeriesInstanceUID);
                Console.WriteLine("Before Delete:");
                await foreach(var dataset in filesBeforeDelete)
                {
                    Console.WriteLine(dataset.GetString(DicomTag.SOPInstanceUID));
                }

                // Delete one of the files
                await dicomServiceApi.DeleteInstanceAsync(StudyInstanceUID, SeriesInstanceUID, SOPInstanceUIDs[3]);

                // List the stored files after delete
                var filesAfterDelete = await dicomServiceApi.RetrieveSeriesMetadataAsync(StudyInstanceUID, SeriesInstanceUID);
                Console.WriteLine($"After Delete instance {SOPInstanceUIDs[3]}:");
                await foreach(var dataset in filesAfterDelete)
                {
                    Console.WriteLine(dataset.GetString(DicomTag.SOPInstanceUID));
                }

                // Delete the whole series
                await dicomServiceApi.DeleteSeriesAsync(StudyInstanceUID, SeriesInstanceUID);

                // Store an instance for the same study, different series
                await PostDicomFileAsync(new DicomDataset()
                {
                    { DicomTag.StudyInstanceUID, StudyInstanceUID }
                });

                var filesAfterDelete2 = await dicomServiceApi.RetrieveStudyMetadataAsync(StudyInstanceUID);
                Console.WriteLine("After Delete series:");
                await foreach(var dataset in filesAfterDelete2)
                {
                    Console.WriteLine(dataset.GetString(DicomTag.SOPInstanceUID));
                }

            }
    }
}