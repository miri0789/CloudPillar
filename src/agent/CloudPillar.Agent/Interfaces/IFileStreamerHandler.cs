namespace CloudPillar.Agent.Interfaces;

public interface IFileStreamerHandler
{
    Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes);

    void DeleteFile(string filePath);

    Task<bool> CheckFileBytesNotEmptyAsync(string filePath, long startPosition, long endPosition);

    Task DeleteFileBytesAsync(string filePath, long startPosition, long endPosition);
}