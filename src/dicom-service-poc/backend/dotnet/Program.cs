namespace DicomBackendPoC
{
    partial class Program
    {  
        static async Task Main(string[] args)
        {
            DicomServiceApiTest dicomApiTests = new DicomServiceApiTest();

            try
            {
                await dicomApiTests.StoreAndRetrieveDicomFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                await dicomApiTests.ValidateDicomDatasets();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                await dicomApiTests.QueryByModality();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                await dicomApiTests.DeleteInstanceTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                await dicomApiTests.DeleteAllStudies();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            await dicomApiTests.StoreDicomWithPatientDetailsChanges();
            await dicomApiTests.StoreDicomAsOriginalCase();
            await dicomApiTests.StoreDicomNotOriginalCase();
        }
    }
}
