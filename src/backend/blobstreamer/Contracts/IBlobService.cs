using Microsoft.Azure.Storage.Blob;

namespace blobstreamer.Contracts
{
    public interface IBlobService
    {
        Task<BlobProperties> GetBlobMeatadataAsync(string filename);
    }
}