namespace CloudPillar.Agent.Wrappers;

public interface IFileStreamerWrapper
{
    Stream CreateStream(string fullFilePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, int BufferSize, bool useAsync);
    FileStream CreateStream(string fullFilePath, FileMode fileMode);

    byte[] ReadStream(string fullFilePath, long startPosition, long lengthToRead);

    DirectoryInfo CreateDirectory(string directoryPath);

    Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes);

    Task WriteAsync(Stream stream, byte[] bytes);

    void SetLength(Stream stream, long length);

    void DeleteFile(string filePath);

    Task<string> ReadAllTextAsync(string filePath);

    Task UnzipFileAsync(string filePath, string destinationPath);

    bool FileExists(string filePath);

    bool DirectoryExists(string fullFilePath);

    string Combine(string baseDir, string path);

    string GetDirectoryName(string filePathPattern);

    string GetFileName(string filePathPattern);

    string GetTempFileName();

    string[] GetFiles(string directoryPath, string searchPattern);

    string[] GetFiles(string fullFilePath, string searchPattern, SearchOption searchOption);

    string[] GetDirectories(string directoryPath, string searchPattern);

    FileStream OpenRead(string filePath);

    string[] Concat(string[] files, string[] directoories);

    string? GetExtension(string path);

    long GetFileLength(string path);
}
