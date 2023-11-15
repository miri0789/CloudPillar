namespace CloudPillar.Agent.Wrappers;

public interface IFileStreamerWrapper
{
    Stream CreateStream(string fullFilePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, int BufferSize, bool useAsync);
    
    FileStream CreateStream(string fullFilePath, FileMode fileMode);

    DirectoryInfo CreateDirectory(string directoryPath);

    FileStream CreateStream(string fullFilePath, FileMode fileMode);

    DirectoryInfo CreateDirectory(string directoryPath);

    Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes);

    void DeleteFile(string filePath);

    Task<bool> HasBytesAsync(string filePath, long startPosition, long endPosition);

    Task<string> ReadAllTextAsync(string filePath);

    bool FileExists(string filePath);

    bool DirectoryExists(string fullFilePath);

    string Combine(string baseDir, string path);

    string GetDirectoryName(string filePathPattern);

    string GetFileName(string filePathPattern);

    string[] GetFiles(string directoryPath, string searchPattern);

    string[] GetFiles(string fullFilePath, string searchPattern, SearchOption searchOption);

    string[] GetDirectories(string directoryPath, string searchPattern);

    string[] Concat(string[] files, string[] directoories);

}
