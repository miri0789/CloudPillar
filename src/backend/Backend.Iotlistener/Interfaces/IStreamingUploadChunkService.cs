namespace Backend.Iotlistener.Interfaces;

public interface IStreamingUploadChunkService
{
    Task UploadStreamToBlob(string deviceId, StreamingUploadChunkEvent data);
}