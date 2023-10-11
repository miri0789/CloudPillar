
using System.Text.RegularExpressions;

namespace CloudPillar.Agent.Wrappers;
public class FileStreamerWrapper : IFileStreamerWrapper
{
    public FileStream CreateStream(string fullFilePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, int BufferSize, bool useAsync)
    {
        return new FileStream(fullFilePath, fileMode, fileAccess, fileShare, BufferSize, useAsync);
    }

    public async Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            fileStream.Seek(writePosition, SeekOrigin.Begin);
            await fileStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public async Task<bool> HasBytesAsync(string filePath, long startPosition, long endPosition)
    {
        if (startPosition > endPosition)
        {
            return true;
        }

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fileStream.Seek(startPosition, SeekOrigin.Begin);
            byte[] buffer = new byte[endPosition - startPosition + 1];
            await fileStream.ReadAsync(buffer, 0, buffer.Length);
            return Array.IndexOf(buffer, (byte)0) == -1;
        }
    }

    public async Task<string> ReadAllTextAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            return await File.ReadAllTextAsync(filePath);
        }
        return null;
    }
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }
    public bool DirectoryExists(string fullFilePath)
    {
        return Directory.Exists(fullFilePath);
    }
    public string Combine(string baseDir, string path)
    {
        return Path.Combine(baseDir, path);
    }

    public string GetDirectoryName(string filePathPattern)
    {
        return Path.GetDirectoryName(filePathPattern) ?? "";
    }
    public string GetFileName(string filePathPattern)
    {
        return Path.GetFileName(filePathPattern);
    }
    public string[] GetFiles(string directoryPath, string searchPattern)
    {
        return Directory.GetFiles(directoryPath, searchPattern);
    }

    public string[] GetFiles(string fullFilePath, string searchPattern, SearchOption searchOption)
    {
        return Directory.GetFiles(fullFilePath, searchPattern, searchOption);
    }
    public string[] GetDirectories(string directoryPath, string searchPattern)
    {
        return Directory.GetDirectories(directoryPath, searchPattern);
    }
    public string[] Concat(string[] files, string[] directoories)
    {
        return files.Concat(directoories).ToArray();
    }

    public string RegexReplace(string fullFilePath, string pattern, string replacment)
    {
        return Regex.Replace(fullFilePath, pattern, replacment);
    }
    public string TextReplace(string fullFilePath, string oldValu, string newValue)
    {
        return fullFilePath.Replace(oldValu, newValue);
    }
}
