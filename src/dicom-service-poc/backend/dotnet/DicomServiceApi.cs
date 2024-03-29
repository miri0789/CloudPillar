using System.Net;
using FellowOakDicom;
using Microsoft.Identity.Client;

namespace DicomBackendPoC
{
    public class DicomServiceApi
    {
        private readonly Microsoft.Health.Dicom.Client.IDicomWebClient client;
        private readonly string webServerUrl ="https://malamdicomworkspace-dicom-service-test.dicom.azurehealthcareapis.com";
        private readonly string tenantId = "63d53a16-04d5-4981-b530-4f38d3b16281"; // tenant-id
        private readonly string clientId = "947edf6d-b41d-4f18-9912-b007670e71b3"; // client-id 
        private readonly string clientSecret = "kaJ8Q~y2~z3meNKe87FV9BFb9bFCdncWOt1Fbb2h"; // client-secret
        private readonly string resource = "https://dicom.healthcareapis.azure.com";

        public DicomServiceApi() 
        {
            string accessToken = GetAccessToken().Result;         
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(webServerUrl);
            client = new DicomWebClient(httpClient);  
            client.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        }
        public async Task<string> GetAccessToken()
        {
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
            return await StoreDicom(dicomFile);
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

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryInstancesAsync(string queryString)
        {
            var response = await client.QueryInstancesAsync(queryString);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to query metadata {queryString}");
            } 

            // Return the enumerator of the metadata
            return response;
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesAsync(string queryString)
        {
            var response = await client.QuerySeriesAsync(queryString);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to query metadata {queryString}");
            } 

            // Return the enumerator of the metadata
            return response;
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QuerySeriesInstanceAsync(string seriesInstanceUid, string queryString)
        {
            var response = await client.QuerySeriesInstanceAsync(seriesInstanceUid, queryString);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to query metadata {queryString}");
            } 

            // Return the enumerator of the metadata
            return response;
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyAsync(string queryString)
        {
            var response = await client.QueryStudyAsync(queryString);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to query metadata {queryString}");
            } 

            // Return the enumerator of the metadata
            return response;
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudyInstanceAsync(string studyInstanceUid, string queryString)
        {
            var response = await client.QueryStudyInstanceAsync(studyInstanceUid, queryString);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to query metadata {queryString}");
            } 

            // Return the enumerator of the metadata
            return response;
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesAsync(string studyInstanceUid, string queryString)
        {
            var response = await client.QueryStudySeriesAsync(studyInstanceUid, queryString);
                
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to query metadata {queryString}");
            } 

            // Return the enumerator of the metadata
            return response;
        }

        public async Task<DicomWebAsyncEnumerableResponse<DicomDataset>> QueryStudySeriesInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string queryString)
        {
            var response = await client.QueryStudySeriesInstanceAsync(studyInstanceUid, seriesInstanceUid, queryString);
              
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to query metadata {queryString}");
            } 

            // Return the enumerator of the metadata
            return response;
        }

        /*public async Task<DicomDataset[]> QueryDicomExpandedStandardTag<T>(DicomTag expendedTag, T value)
        {
            var entry = new AddExtendedQueryTagEntry{Path = expendedTag.GetPath(), VR = expendedTag.GetDefaultVR().Code};
            var extendedTagexist = await client.GetExtendedQueryTagAsync(expendedTag.GetPath());
            if(extendedTagexist.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                try
                {
                    await client.AddExtendedQueryTagAsync(new List <AddExtendedQueryTagEntry>{entry});
                }
                catch (System.Exception e)
                {
                    throw new Exception($"Failed to add extened query tag, {e.Message}", e);
                }
            }

            var result = await client.GetExtendedQueryTagAsync(expendedTag.GetPath());
            if(result.Status != ExtendedQueryTagStatus.Ready)
            {
                throw new Exception($"get extended query tag status is: {result.Status}");
            }

            if(result.QueryStatus != QueryStatus.Enabled)
            {
                throw new Exception($"get extended query tag QueryStatus is Disable");
            }

            try
            {
                DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = 
                    await client.QueryInstancesAsync($"{expendedTag.GetPath()}={value}");
                DicomDataset[] instances = await queryResponse.ToArrayAsync();
                return instances;
            }
            catch (System.Exception e)
            {
                throw new Exception($"Failed to query extended tag, {e.Message}", e);
            }
        }*/

        public async Task<GetExtendedQueryTagEntry> GetExtendedQueryTagAsync(string tagPath)
        {
            var response = await client.GetExtendedQueryTagAsync(tagPath);
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get extended tag data: {tagPath}");
            }

            return await response.GetValueAsync();
        }

        public async Task<DicomOperationReference> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> tagEntries)
        {
            var response = await client.AddExtendedQueryTagAsync(tagEntries);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Failed to add extended tag, status code: {response.StatusCode}");
            }
            return await response.GetValueAsync();
        }

        public async Task<HttpStatusCode> DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            var response = await client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            if(response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to delete instance");
            }

            return response.StatusCode;
        }

        public async Task<HttpStatusCode> DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid)
        {
            var response = await client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);

            if(response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to delete Series");
            }

            return response.StatusCode;
        }

        public async Task<DicomWebResponse> DeleteStudyAsync(string studyInstanceUid)
        {
            var response = await client.DeleteStudyAsync(studyInstanceUid);

            if(response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to delete Study");
            }

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

        /// <summary>
        /// Set value to dicom standard tag.
        /// </summary>
        /// <param name="dicomFile">The Dicom file to change.</param>
        /// <param name="dicomTag">The Dicom tag to add.</param>
        /// <param name="value">The value to set in tag.</param>
        /// <returns>The Dicom file with the new tag.</returns>
        public DicomFile SetDicomStandardTag<T>(DicomFile dicomFile, DicomTag dicomTag, params T[] value)
        {
            DicomDataset ds= new DicomDataset();
            ds.AddOrUpdate(dicomTag, value);
            dicomFile.Dataset.AddOrUpdate(ds);
            return dicomFile;
        }
        
        public async Task<DicomWebResponse<DicomDataset>> StoreDicomWithValidation(DicomFile dicomFile)
        {
            try
            {
                if(await PatientMetadataValidation(dicomFile.Dataset))
                {
                    var response = await client.StoreAsync(dicomFile);
                    return response;
                }
                else
                    throw new Exception("You cannot change patient details");
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message}", e);
            }    
        }

        public async Task<bool> PatientMetadataValidation(DicomDataset dataset)
        {
            string query = $"{DicomTag.StudyInstanceUID.GetPath()}={dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)}&includefield={DicomTag.PatientAge.GetPath()}";
            var response = await client.QueryStudyAsync(query);
            var instances = await response.ToArrayAsync();
            string value;
            if(instances.Length > 0){
                if(instances.Last().TryGetSingleValue<string>(DicomTag.PatientID, out value))
                    if(value!= dataset.GetSingleValue<string>(DicomTag.PatientID))
                        return false;
                if(instances.Last().TryGetSingleValue<string>(DicomTag.PatientName, out value))
                    if(value!= dataset.GetSingleValue<string>(DicomTag.PatientName))
                        return false;
                if(instances.Last().TryGetSingleValue<string>(DicomTag.PatientBirthDate, out value))
                    if(value!= dataset.GetSingleValue<string>(DicomTag.PatientBirthDate))
                        return false;
                if(instances.Last().TryGetSingleValue<string>(DicomTag.PatientAge, out value))
                    if(value!= dataset.GetSingleValue<string>(DicomTag.PatientAge))
                        return false;
                if(instances.Last().TryGetSingleValue<string>(DicomTag.PatientSex, out value))
                    if(value!= dataset.GetSingleValue<string>(DicomTag.PatientSex))
                        return false;
            }
            return true;
        }
    }
}