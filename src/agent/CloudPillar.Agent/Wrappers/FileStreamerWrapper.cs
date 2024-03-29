﻿using System.IO.Compression;
using System.Security.AccessControl;
using System.Text;

namespace CloudPillar.Agent.Wrappers;
public class FileStreamerWrapper : IFileStreamerWrapper
{
    public Stream CreateStream(string fullFilePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, int BufferSize, bool useAsync)
    {
        return new FileStream(fullFilePath, fileMode, fileAccess, fileShare, BufferSize, useAsync);
    }

    public Stream CreateStream(string fullFilePath, FileMode fileMode, FileAccess fileAccess)
    {
        return new FileStream(fullFilePath, fileMode, fileAccess);
    }

    public FileStream CreateStream(string fullFilePath, FileMode fileMode)
    {
        return new FileStream(fullFilePath, fileMode);
    }
    public byte[] ReadStream(string fullFilePath, long startPosition, long lengthToRead)
    {
        byte[] data = new byte[lengthToRead];
        using (Stream stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read))
        {
            stream.Seek(startPosition, SeekOrigin.Begin);
            stream.Read(data, 0, (int)lengthToRead);
        }
        return data;
    }

    public int Read(FileStream fileStream, byte[] buffer, int offset, int count)
    {
        return fileStream.Read(buffer, offset, count);
    }

    public async Task WriteAsync(Stream stream, byte[] bytes)
    {
        await stream.WriteAsync(bytes);
    }

    public void SetLength(Stream stream, long length)
    {
        stream.SetLength(length);
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


    public void DeleteFolder(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public bool DirectoryExists(string fullFilePath)
    {
        return Directory.Exists(fullFilePath);
    }

    public bool HasExtension(string fullFilePath)
    {
        return Path.HasExtension(fullFilePath);
    }

    public string GetFullPath(string fullFilePath)
    {
        return Path.GetFullPath(fullFilePath);
    }


    public DirectoryInfo CreateDirectoryInfo(string directoryPath)
    {
        return new DirectoryInfo(directoryPath);
    }

    public DirectorySecurity GetAccessControl(DirectoryInfo directoryInfo)
    {
        return directoryInfo.GetAccessControl();
    }

    public AuthorizationRuleCollection GetAccessRules(DirectorySecurity directorySecurity)
    {
        return directorySecurity.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
    }

    public bool isSpaceOnDisk(string path, long size)
    {
        DriveInfo drive = new DriveInfo(path);
        return drive.AvailableFreeSpace > size;
    }

    public string Combine(string baseDir, string path)
    {
        return Path.Combine(baseDir, path);
    }

    public string GetDirectoryName(string filePathPattern)
    {
        return Path.GetDirectoryName(filePathPattern);
    }

    public string GetPathRoot(string filePathPattern)
    {
        return Path.GetPathRoot(filePathPattern);
    }

    public string GetFileName(string filePathPattern)
    {
        return Path.GetFileName(filePathPattern);
    }

    public string GetFileNameWithoutExtension(string filePath)
    {
        return Path.GetFileNameWithoutExtension(filePath);
    }

    public string GetTempPath()
    {
        return Path.GetTempPath();
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

    public FileStream OpenRead(string filePath)
    {
        return File.OpenRead(filePath);
    }

    public void CreateDirectory(string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);
    }

    public ZipArchive OpenZipFile(string filePath)
    {
        return ZipFile.Open(filePath, ZipArchiveMode.Read, Encoding.UTF8);
    }
    public string? GetExtension(string path)
    {
        return Path.GetExtension(path);
    }
    public long GetFileLength(string path)
    {
        FileInfo fileInfo = new FileInfo(path);
        return fileInfo.Exists ? fileInfo.Length : 0;
    }    

    public Stream OpenZipArchiveEntry(ZipArchiveEntry zipArchiveEntry)
    {
        return zipArchiveEntry.Open();
    }

    public void SetLastWriteTimeUtc(string filePath, DateTime lastWriteTime)
    {
        File.SetLastWriteTimeUtc(filePath, lastWriteTime);
    }

    public FileStream FileCreate(string filePath)
    {
        return File.Create(filePath);
    }



}
