using Shared.Entities.Messages;

namespace Backend.Iotlistener.Interfaces;

public interface IStreamingUploadChunkService
{
    Task UploadStreamToBlob(streamingUploadChunkEvent data);
}