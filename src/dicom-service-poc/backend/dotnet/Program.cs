using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DicomBackendPoC
{

    partial class Program
    {  
        static async Task Main(string[] args)
        {
            DicomServiceApi dicomServiceApi = new DicomServiceApi();
            
            try
            {
                await dicomServiceApi.StoreDicom("C:\\Dev\\dcm\\minimal_01.dcm");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while storing the dicom file: " + ex.Message);
            }
            

            try
            {
                var dicomFile = await dicomServiceApi.RetrieveInstance("12.3.6468.789454613000", "36541287984.3000", "1187945.3.6458.12000");
                dicomFile.Save("C:\\Dev\\dcm\\minimal_01_retrieved.dcm");
                Console.WriteLine("File saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while retrieving the dicom file: " + ex.Message);
            }
            

        }

    }

}
