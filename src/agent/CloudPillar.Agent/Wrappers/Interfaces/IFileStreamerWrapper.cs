using System.Security.AccessControl;

namespace CloudPillar.Agent.Wrappers;

public interface IFileStreamerWrapper
{
    Stream CreateStream(string fullFilePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, int BufferSize, bool useAsync);

    Stream CreateStream(string fullFilePath, FileMode fileMode, FileAccess fileAccess);
    FileStream CreateStream(string fullFilePath, FileMode fileMode);

    int Read(FileStream fileStream, byte[] buffer, int offset, int count);

    byte[] ReadStream(string fullFilePath, long startPosition, long lengthToRead);

    void CreateDirectory(string destinationPath);

    DirectoryInfo CreateDirectoryInfo(string directoryPath);

    DirectorySecurity GetAccessControl(DirectoryInfo directoryInfo);

    AuthorizationRuleCollection GetAccessRules(DirectorySecurity directorySecurity);

    Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes);

    Task WriteAsync(Stream stream, byte[] bytes);

    void SetLength(Stream stream, long length);

    void DeleteFile(string filePath);

    Task<string> ReadAllTextAsync(string filePath);

    Task UnzipFileAsync(string filePath, string destinationPath);

    bool FileExists(string filePath);

    bool DirectoryExists(string fullFilePath);

    public bool isSpaceOnDisk(string path, long size);

    string Combine(string baseDir, string path);

    string GetDirectoryName(string filePathPattern);

    string GetPathRoot(string filePathPattern);

    string GetFileName(string filePathPattern);
    string GetFileNameWithoutExtension(string filePath);

    string GetTempPath();
    string[] GetFiles(string directoryPath, string searchPattern);

    string[] GetFiles(string fullFilePath, string searchPattern, SearchOption searchOption);

    string[] GetDirectories(string directoryPath, string searchPattern);

    FileStream OpenRead(string filePath);

    string[] Concat(string[] files, string[] directoories);

    string? GetExtension(string path);

    long GetFileLength(string path);

    bool HasExtension(string fullFilePath);

    string GetFullPath(string fullFilePath);
}
