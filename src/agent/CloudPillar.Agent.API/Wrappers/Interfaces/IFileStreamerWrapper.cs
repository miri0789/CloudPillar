namespace CloudPillar.Agent.API.Wrappers;

public interface IFileStreamerWrapper
{
    Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes);

    void DeleteFile(string filePath);

    Task<bool> HasBytesAsync(string filePath, long startPosition, long endPosition);
}