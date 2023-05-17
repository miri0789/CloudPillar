using System;
using Microsoft.Health.Dicom.Client;
using Azure.Identity;
using Azure.Core;
using FellowOakDicom;
using Microsoft.Identity.Client;

namespace DicomAgentPoC
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            DicomManager.DicomServiceConnection();

            //Store a DICOM with zip
            var dcmImage = @"C:\Dev\minimal_01.dcm";
            var zipFile = @"C:\Dev\Catheter-501.zip";
            // Read the contents of the zip file into a byte array
            byte[] zipData = File.ReadAllBytes(zipFile);
            await DicomManager.StoreDicomWithBinaryTag(dcmImage, zipData);
        }
    }
}