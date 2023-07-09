using Microsoft.Health.Dicom.Client;
using FellowOakDicom;
using System.Text;
using System.Text.Json;

namespace DicomBackendPoC
{
    public class DicomLifeCycleManager
    {
        private readonly DicomServiceApi dicomService;
        private HttpClient client = new HttpClient(); 


        public DicomLifeCycleManager(DicomServiceApi service)
        {
            dicomService = service;
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryOutdatedDicomFiles(
            DateTime startDate =  default(DateTime), DateTime endDate =  default(DateTime))
        {
            // by default export the dicom files from the last 10 years (excluding the files from last 3 months)

            if(startDate ==  default(DateTime))
            {
                startDate = DateTime.Now.AddYears(-10);
            }
            if(endDate ==  default(DateTime))
            {
                endDate = DateTime.Now.AddMonths(-3);
            }
             
            string query = $"StudyDate={startDate.ToString("yyyyMMdd")}-{endDate.ToString("yyyyMMdd")}";
            var response = await dicomService.QueryStudyAsync(query);
            if(response.IsSuccessStatusCode)
            {
                return response;
            }
            throw new Exception("Query Outdated studies failed.");
        }

        private async Task<HttpResponseMessage> ExportDicomsToBlob(List<DicomDataset> filesToExport)
        {        
            List<string> studyUIDList = new List<string>();
            foreach(var file in filesToExport)
            {
                studyUIDList.Add(file.GetString(DicomTag.StudyInstanceUID));
            }
            var values = string.Join("\", \"", studyUIDList);

            var requestContent = "{{\"source\": {{\"type\": \"identifiers\",\"settings\": {{\"values\": [" +
                                "\"{0}\"]}}}}," +
                                "\"destination\": {{\"type\": \"azureblob\",\"settings\": {{" +
                                "\"blobContainerUri\": \"https://iotdicomdevstorage.blob.core.windows.net/dicomcontainer\"," +
                                "\"UseManagedIdentity\": true}}}}}}";

            var content = string.Format(requestContent, values);

            var url = "https://malamdicomworkspace-dicom-service-test.dicom.azurehealthcareapis.com/v1/export";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(content, Encoding.UTF8, "application/json") })
            {
                var accessToken = dicomService.GetAccessToken().Result;
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.SendAsync(request);
                Console.WriteLine($"Got {response.StatusCode}");
                return response;
            }                
        }
        
        private async Task DeleteOutdatedDicomsFromService(List<DicomDataset> OutdatedFiles)
        {
            foreach(var file in OutdatedFiles)
            {
                await dicomService.DeleteStudyAsync(file.GetString(DicomTag.StudyInstanceUID));
            }
        }

        public async Task ExportAndDeleteOutdatedDicoms(
            DateTime startDate =  default(DateTime), DateTime endDate =  default(DateTime))
        {
            try
            {
                var OutdatedStudied = await QueryOutdatedDicomFiles(startDate, endDate);
                var result = await ExportDicomsToBlob(await OutdatedStudied.ToListAsync());

                if (result.IsSuccessStatusCode)
                {
                    var operationLocation = result.Headers.GetValues("Location").FirstOrDefault();
                    
                    // Poll the status of the export operation until it is completed
                    while (true)
                    {
                        await Task.Delay(500); 

                        // Check the status of the export operation
                        var accessToken = dicomService.GetAccessToken().Result;
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                        var operationResponse = await client.GetAsync(operationLocation);
                        string operationStatus = await operationResponse.Content.ReadAsStringAsync();

                        if(operationStatus != null)
                        {
                            // Parse the JSON response into a JsonDocument
                            JsonDocument jsonDocument = JsonDocument.Parse(operationStatus);
                            string status = "";
                            // Access the value of the "status" field
                            if (jsonDocument.RootElement.TryGetProperty("status", out JsonElement statusElement))
                            {
                                if (statusElement.ValueKind == JsonValueKind.String)
                                {
                                    status = statusElement.GetString();
                                    Console.WriteLine($"Status: {status}");
                                }
                                else
                                {
                                    Console.WriteLine("Invalid status value.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Status property not found.");
                            }   

                            if (status == "completed")
                            {
                                // Export operation completed successfully
                                await DeleteOutdatedDicomsFromService(await OutdatedStudied.ToListAsync());
                                break;
                            }
                            else if (status == "failed")
                            {
                                // Export operation failed
                                throw new Exception("Export operation failed.");
                            }
                        }
                    }
                }  
                else
                {
                    throw new Exception("Export operation failed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting DICOM files to Blob Storage: {ex.Message}");
            }
        }
    }
}