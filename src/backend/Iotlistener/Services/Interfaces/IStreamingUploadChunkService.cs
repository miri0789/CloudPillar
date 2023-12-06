using Shared.Entities.Messages;

namespace Backend.Iotlistener.Interfaces;

public interface IStreamingUploadChunkService
{
    Task UploadStreamToBlob(StreamingUploadChunkEvent data, string deviceId);
}