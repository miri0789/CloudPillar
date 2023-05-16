using Microsoft.Health.Dicom.Client;
using Azure.Identity;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using FellowOakDicom;
//using FellowOakDicom.DicomDataSet;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client; // I am using the Package Microsoft.Identity.Client from nuget.org
using System;
using System.Net;

namespace DicomBackendPoC
{
    public class DicomServiceApi
    {
        private readonly Microsoft.Health.Dicom.Client.IDicomWebClient client;

        public DicomServiceApi() 
        {
            string accessToken = GetAccessToken().Result;
            string webServerUrl ="https://malamdicomworkspace-dicom-service-test.dicom.azurehealthcareapis.com";
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(webServerUrl);
            client = new DicomWebClient(httpClient);  
            client.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        }

        private async Task<string> GetAccessToken()
        {
            // OAuth necessary Information
            string tenantId = "63d53a16-04d5-4981-b530-4f38d3b16281"; // tenant-id
            string clientId = "947edf6d-b41d-4f18-9912-b007670e71b3"; // client-id 
            string clientSecret = "kaJ8Q~y2~z3meNKe87FV9BFb9bFCdncWOt1Fbb2h"; // client-secret
            string resource = "https://dicom.healthcareapis.azure.com";

            // Construct the authority and token endpoints
            string authority = $"https://login.microsoftonline.com/{tenantId}";

            // Create a confidential client application object with your app id and secret
            IConfidentialClientApplication app;
            app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            // Set the Scope to our Dicom Server
            var scopes = new[] { $"{resource}/.default" };

            // Request the Bearer Token
            var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            // Return The Access Token
            return authResult.AccessToken;
        }

        public async Task<HttpStatusCode> StoreDicom(string fileName)
        {
            var dicomFile = await DicomFile.OpenAsync(fileName);
            try
            {
                var response = await client.StoreAsync(dicomFile);

                return response.StatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}, file name: {fileName}", ex);
            }    
        }

        public async Task<HttpStatusCode> StoreDicom(DicomFile dicomFile)
        {
            try
            {
                var response = await client.StoreAsync(dicomFile);

                return response.StatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }    
        }

        public async Task<DicomFile> RetrieveInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            try
            {
                var response = await client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                if (response.IsSuccessStatusCode)
                {
                    // Get the response content as a byte array
                    var content = await response.Content.ReadAsByteArrayAsync();

                    // Create a DicomFile object from the response content
                    var dicomFile = DicomFile.Open(new MemoryStream(content));

                    return dicomFile;
                }
                else
                {
                    throw new Exception($"Instance not found: {studyInstanceUid}, {seriesInstanceUid}, {sopInstanceUid}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve instance: {studyInstanceUid}, {seriesInstanceUid}, {sopInstanceUid}", ex);
            }

        }

        public async Task<DicomDataset> RetrieveInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {

            var response = await client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve matadata for instance {sopInstanceUid}");
            } 
                
            var enumerator = response.GetAsyncEnumerator();
            try
            {
                // Move to the first DicomDataset in the response
                if(await enumerator.MoveNextAsync())
                {
                    return enumerator.Current;
                }
                throw new Exception($"Failed to retrieve matadata for instance {sopInstanceUid}");
            }
            finally
            {
                // Dispose the enumerator in any case
                await enumerator.DisposeAsync();
            }       
            
        }

        // This function uses asynchronous enumerator to retrieve the instances of the series one at a time when they are downloaded.
        // It is efficient for dealling with large series because it allows processing each instance
        // as soon as it is available without waiting to the entire series to download.
        public async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveSeriesAsync(string studyInstanceUid, string seriesInstanceUid, string dicomTransferSyntax = "*")
        {
            var response = await client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve series {seriesInstanceUid}");
            } 

            // Return the enumerator of the retrieved series Dicom files
            return response;   
        }

        public async Task<List<DicomFile>> RetrieveSeriesDicomList(string studyInstanceUid, string seriesInstanceUid, string dicomTransferSyntax = "*")
        {
            var response = await client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve series {seriesInstanceUid}");
            } 

            return await GetDicomFilesListFromEnumerator(response);
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveSeriesMetadataAsync(string studyInstanceUid, string seriesInstanceUid)
        {
            var response = await client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve series metadata for {seriesInstanceUid}");
            } 

            // Return the enumerator of the retrieved series metadata
            return response;   
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomFile>> RetrieveStudyAsync(string studyInstanceUid, string dicomTransferSyntax = "*")
        {
            var response = await client.RetrieveStudyAsync(studyInstanceUid, dicomTransferSyntax);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve study {studyInstanceUid}");
            } 

            // Return the enumerator of the retrieved study
            return response; 
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> RetrieveStudyMetadataAsync(string studyInstanceUid)
        {
            var response = await client.RetrieveStudyMetadataAsync(studyInstanceUid);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve study metadata {studyInstanceUid}");
            } 

            // Return the enumerator of the retrieved study metadata
            return response;
        }

        public async Task<List<DicomFile>> GetDicomFilesListFromEnumerator(DicomWebAsyncEnumerableResponse<DicomFile> response)
        {
            var files = new List<DicomFile>();
            var enumerator = response.GetAsyncEnumerator();
            try
            {
                // Move to the next Dicom file in the response
                while (await enumerator.MoveNextAsync())
                {
                    var instance = enumerator.Current;
                    if (instance != null && instance.FileMetaInfo != null)
                    {
                        files.Add(instance);
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
            return files;
        }

        public async Task<List<DicomDataset>> GetDicomDatasetsListFromEnumerator(DicomWebAsyncEnumerableResponse<DicomDataset> response)
        {
            var datasets = new List<DicomDataset>();
            var enumerator = response.GetAsyncEnumerator();
            try
            {
                // Move to the next Dicom file in the response
                while (await enumerator.MoveNextAsync())
                {
                    var instance = enumerator.Current;
                    if (instance != null)
                    {
                        datasets.Add(instance);
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
            return datasets;
        }
    }
}