using Shared.Entities.Events;

namespace Backend.Iotlistener.Interfaces;

public interface IStreamingUploadChunkService
{
    Task UploadStreamToBlob(StreamingUploadChunkEvent data);
}