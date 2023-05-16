using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Identity.Client;

namespace DicomAgentPoC
{
    public class DicomManager
    {
        private static Microsoft.Health.Dicom.Client.IDicomWebClient client;
        private static readonly string webServerUrl = "https://malamdicomworkspace-dicom-service-test.dicom.azurehealthcareapis.com";

        /// <summary>
        /// Connect to Dicom service using DicomWebClient.
        /// </summary>
        /// <param name="webServerUrl">The web Server Url.</param>
        public static void DicomServiceConnection()
        {
            var accessToken = GetAccessToken().Result;

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(webServerUrl);
            client = new DicomWebClient(httpClient);
            client.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        /// <summary>
        /// Get Access Token for specific app client with client secret.
        /// </summary>
        /// <returns>The Access Token.</returns>
        private static async Task<string> GetAccessToken()
        {
            // OAuth necessary Information
            string tenantId = "63d53a16-04d5-4981-b530-4f38d3b16281"; // tenant-id
            string clientId = "947edf6d-b41d-4f18-9912-b007670e71b3"; // client-id 
            string clientSecret = "kaJ8Q~y2~z3meNKe87FV9BFb9bFCdncWOt1Fbb2h";
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

            // Return The Access Tokem
            return authResult.AccessToken;
        }

        /// <summary>
        /// Store Dicom with binary tag in Dicom Service.
        /// </summary>
        /// <param name="dicomFilePath">The Dicom file to update the new binary tag.</param>
        /// <param name="binaryData">The binary data to store in tag.</param>
        public static async Task StoreDicomWithBinaryTag(string dicomFilePath, byte[] binaryData)
        {
            // Open a new DICOM dataset and add the binary data element to it
            var dicomFile = await DicomFile.OpenAsync(dicomFilePath);

            var zipDicomTag = new DicomTag(0x0009, 0x1004, "Biosense");

            // Create a new binary data element for the private tag
            var dicomWithZipTag = AddDicomBinaryTag(dicomFile, zipDicomTag, binaryData);
            var response = await client.StoreAsync(dicomWithZipTag);
            Console.WriteLine($"{dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)} saved with status code: {response.StatusCode}");
        }

        /// <summary>
        /// Create a new binary data element for the private tag and add it to the Dicom.
        /// </summary>
        /// <param name="dicomFile">The Dicom file to change.</param>
        /// <param name="dicomTag">The Dicom tag to add (group, element, privateCreator).</param>
        /// <param name="binaryData">The binary data to store in tag.</param>
        /// <returns>The Dicom file with the new tag.</returns>
        public static DicomFile AddDicomBinaryTag(DicomFile dicomFile, DicomTag dicomTag, byte[] binaryData)
        {
            // Create a new binary data element for the private tag
            DicomElement element = new DicomOtherByte(dicomFile.Dataset.GetPrivateTag(dicomTag), binaryData);
            dicomFile.Dataset.Add(element);
            return dicomFile;
        }
    }
}