using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;

namespace DicomAgentPoC
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            DicomManagerTest managerTest = new DicomManagerTest();

            await managerTest.StoreDicomWithBinaryTag();
            await managerTest.QueryDicomWithExpandedStandardTag();
            await managerTest.QueryDicomWithExpandedPrivateTag();
            await managerTest.StoreDicomWithPatientDetailsChanges();
        }
    }
}