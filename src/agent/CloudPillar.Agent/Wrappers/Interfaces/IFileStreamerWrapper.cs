namespace CloudPillar.Agent.Wrappers;

public interface IFileStreamerWrapper
{
    Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes);

    void DeleteFile(string filePath);

    Task<bool> HasBytesAsync(string filePath, long startPosition, long endPosition);

    Task<string> ReadAllTextAsync(string filePath);
}