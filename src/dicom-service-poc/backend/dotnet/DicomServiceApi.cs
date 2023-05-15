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
               // .WithTenantId(tenantId)
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
                // Call StoreAsync to store the instance
                var response = await client.StoreAsync(dicomFile);

                return response.StatusCode;
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the store process
                throw new Exception($"Failed to store file: {fileName}", ex);
            }    
        }

        public async Task<DicomFile> RetrieveInstance(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            try
            {
                // Call RetrieveInstanceAsync to retrieve the instance
                var response = await client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Get the response content as a byte array
                    var content = await response.Content.ReadAsByteArrayAsync();

                    // Create a DicomFile object from the response content
                    var dicomFile = DicomFile.Open(new MemoryStream(content));

                    // Return the DicomFile object
                    return dicomFile;
                }
                else
                {
                    // Handle the case where the instance was not found
                    throw new Exception($"Instance not found: {studyInstanceUID}, {seriesInstanceUID}, {sopInstanceUID}");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the retrieval process
                throw new Exception($"Failed to retrieve instance: {studyInstanceUID}, {seriesInstanceUID}, {sopInstanceUID}", ex);
            }

        }

    }
}