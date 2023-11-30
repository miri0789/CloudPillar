namespace CloudPillar.Agent.Handlers
{
    public class ResumeUploadHandler : IResumeUploadHandler
    {
        public ResumeUploadHandler()
        {
        }

        public float CalculateByteUploadedPercent(long bytesToUpload, long currentPosition, long streamLength)
        {
            bytesToUpload += currentPosition;
            var percentage = CalculateByteUploadedPercent(streamLength, bytesToUpload);
            return percentage;
        }

        public float CalculateByteUploadedPercent(long bytesToUpload, long streamLength)
        {
            float progressPercent = (float)Math.Floor(bytesToUpload / (double)streamLength * 100 * 100) / 100;
            Console.WriteLine($"Upload Progress: {progressPercent:F2}%");
            return progressPercent;
        }

        public int CalculateCurrentPosition(float streamLength, float progressPercent)
        {
            if (progressPercent == 0)
            {
                return 0;
            }
            int currentPosition = (int)Math.Floor(progressPercent * (float)streamLength / 100);

            Console.WriteLine($"Current Position: {currentPosition} bytes");
            return currentPosition;
        }

    }
}