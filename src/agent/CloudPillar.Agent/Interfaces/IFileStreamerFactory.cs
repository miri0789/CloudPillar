namespace CloudPillar.Agent.Interfaces;

public interface IFileStreamerFactory
{
    Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes);

    void DeleteFile(string filePath);

    Task<bool> HasBytesAsync(string filePath, long startPosition, long endPosition);
}