

namespace CloudPillar.Agent.Handlers
{
    public interface IResumeUploadHandler
    {
        float CalculateByteUploadedPercent(long bytesToUpload, long currentPosition, long streamLength);
        float CalculateByteUploadedPercent(long bytesToUpload, long streamLength);
        int CalculateCurrentPosition(float streamLength, float progressPercent);
    }

}