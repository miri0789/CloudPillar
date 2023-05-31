using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Identity.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;

namespace DicomAgentPoC
{
    public class DicomManager
    {
        private static Microsoft.Health.Dicom.Client.IDicomWebClient client;
        private static readonly string webServerUrl = "https://malamdicomworkspace-dicom-service-test.dicom.azurehealthcareapis.com";
        private static readonly string tenantId = "63d53a16-04d5-4981-b530-4f38d3b16281";
        private static readonly string clientId = "947edf6d-b41d-4f18-9912-b007670e71b3";
        private static readonly string clientSecret = "kaJ8Q~y2~z3meNKe87FV9BFb9bFCdncWOt1Fbb2h";
        private static readonly string resource = "https://dicom.healthcareapis.azure.com";
        private static readonly string login = "https://login.microsoftonline.com";

        /// <summary>
        /// Connect to Dicom service using DicomWebClient.
        /// </summary>
        public void DicomServiceConnection()
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
        private async Task<string> GetAccessToken()
        {
            // Construct the authority and token endpoints
            string authority = $"{login}/{tenantId}";
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

        /// <summary>
        /// Expand dicom with new private tag.
        /// </summary>
        /// <param name="dicomFile">The Dicom file to change.</param>
        /// <param name="dicomTag">The Dicom tag to add (group, element, PrivateCreator).</param>
        /// <param name="value">The value to set in tag.</param>
        /// <returns>The Dicom file with the new tag.</returns>
        public DicomFile ExpandDicomWithNewPrivateTag<T>(DicomFile dicomFile, DicomTag dicomTag, DicomVR vr, params T[] value)
        {
            // Create a data set for the private tag and add it to the dicom file
            // important! to save the file coorectly with the vr of the privat tag so it will be queryable need to set FileMetaInfo.TransferSyntaxUID to ExplicitVRLittleEndian.
            DicomDataset ds= new DicomDataset();

            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            ds.AddOrUpdate(vr, ds.GetPrivateTag(dicomTag), value);

            dicomFile.Dataset.AddOrUpdate(ds);
            return dicomFile;
        }

        public async Task<DicomDataset[]> QueryDicomExpandedPrivateTag<T>(DicomTag expendedTag, DicomVR tagVr, QueryTagLevel tagLevel, T value)
        {
            var dataset = new DicomDataset();

            var entry = new AddExtendedQueryTagEntry{Path = dataset.GetPrivateTag(expendedTag).GetPath(), VR = tagVr.Code, Level = tagLevel, PrivateCreator = expendedTag.PrivateCreator.ToString()};
            var extendedTagexist = await GetExtendedQueryTagAsync(dataset.GetPrivateTag(expendedTag).GetPath());
            if(extendedTagexist.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                try
                {
                    await AddExtendedQueryTagAsync(new List <AddExtendedQueryTagEntry>{entry});
                }
                catch (System.Exception e)
                {
                    throw new Exception($"Failed to add extened query tag, {e.Message}", e);
                }
            }

            var result = await GetExtendedQueryTagAsync(dataset.GetPrivateTag(expendedTag).GetPath());
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
                DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse= await client.QueryInstancesAsync($"{dataset.GetPrivateTag(expendedTag).GetPath()}={value}");
                DicomDataset[] instances = await queryResponse.ToArrayAsync();
                return instances;
            }
            catch (System.Exception e)
            {
                throw new Exception($"Failed to query extended tag, {e.Message}", e);
            }
        }

        public async Task<DicomDataset[]> QueryDicomExpandedStandardTag<T>(DicomTag expendedTag, T value)
        {
            var entry = new AddExtendedQueryTagEntry{Path = expendedTag.GetPath(), VR = expendedTag.GetDefaultVR().Code};
            var extendedTagexist = await client.GetExtendedQueryTagAsync(expendedTag.GetPath());
            if(extendedTagexist.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                try
                {
                    await AddExtendedQueryTagAsync(new List <AddExtendedQueryTagEntry>{entry});
                }
                catch (System.Exception e)
                {
                    throw new Exception($"Failed to add extened query tag, {e.Message}", e);
                }
            }

            var result = await GetExtendedQueryTagAsync(expendedTag.GetPath());
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
                DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse= await client.QueryInstancesAsync($"{expendedTag.GetPath()}={value}");
                DicomDataset[] instances = await queryResponse.ToArrayAsync();
                return instances;
            }
            catch (System.Exception e)
            {
                throw new Exception($"Failed to query extended tag, {e.Message}", e);
            }
        }


        // ---Dicom API Functions---
        private async Task<GetExtendedQueryTagEntry> GetExtendedQueryTagAsync(string tagPath)
        {
            var response = await client.GetExtendedQueryTagAsync(tagPath);
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get extended tag: {tagPath}");
            }
            return await response.GetValueAsync();
        }

        private async Task<DicomOperationReference> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> tagEntries)
        {
            var response = await client.AddExtendedQueryTagAsync(tagEntries);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Failed to add extended tag, status code: {response.StatusCode}");
            }
            return await response.GetValueAsync();
        }

        private async Task DeleteExtendedQueryTagAsync(string tagPath)
        {
            var response = await client.DeleteExtendedQueryTagAsync(tagPath);
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to delete extended tag: {tagPath}");
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreDicom(DicomFile dicomFile)
        {
            try
            {
                var response = await client.StoreAsync(dicomFile);
                return response;
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message}", e);
            }    
        }
    }
}