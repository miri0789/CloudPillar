namespace DicomAgentPoC
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            DicomManagerTest managerTest = new DicomManagerTest();

            try
            {
                await managerTest.StoreDicomWithBinaryTag();
            }
            catch (System.Exception e)
            {
                
                Console.WriteLine(e.Message);
            }

            try
            {
                await managerTest.QueryDicomWithExpandedStandardTag();
            }
            catch (System.Exception e)
            {
                
                Console.WriteLine(e.Message);
            }

            try
            {
                await managerTest.QueryDicomWithExpandedPrivateTag();
            }
            catch (System.Exception e)
            {
                
                Console.WriteLine(e.Message);
            }
            
        }
    }
}