
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

// public abstract class FileSystemInfoBase
// {
//     public abstract string Name { get; }
//     public abstract string FullName { get; }
// }

// public abstract class DirectoryInfoBase : FileSystemInfoBase
// {
//     public abstract IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos();
//     public abstract DirectoryInfoBase? GetDirectory(string path);
//     public abstract FileInfoBase? GetFile(string path);
//     public abstract DirectoryInfoBase? ParentDirectory { get; }
// }

// public abstract class FileInfoBase : FileSystemInfoBase
// {
//     public abstract DirectoryInfoBase ParentDirectory { get; }
// }

public class InMemoryFileInfo : FileInfoBase
{
    private readonly string _fileName;

    public InMemoryFileInfo(string fileName)
    {
        _fileName = fileName;
    }

    public override string Name => _fileName;
    public override string FullName => _fileName;
    public override DirectoryInfoBase ParentDirectory => new InMemoryDirectoryInfo(_fileName.Substring(0, _fileName.LastIndexOf('/')));
}

public class InMemoryDirectoryInfo : DirectoryInfoBase
{
    private readonly List<string> _fileNames;
    private readonly string _directoryName;

    public InMemoryDirectoryInfo(List<string> fileNames, string rootPath)
    {
        _fileNames = fileNames;
        _directoryName = rootPath;
    }

    public InMemoryDirectoryInfo(string directoryName)
    {
        // This constructor is used by the InMemoryFileInfo's ParentDirectory property.
        // It's not used for actual matching in this example.
        _fileNames = new List<string>();
        _directoryName = directoryName;
    }

    public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
    {
        foreach (var fileName in _fileNames)
        {
            if (fileName.StartsWith(_directoryName))
            {
                yield return new InMemoryFileInfo(fileName);
            }
        }
    }
    //  public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
    // {
    //     foreach (var fileName in _fileNames)
    //     {
    //         var relativePath = fileName.Substring(_directoryName.Length);
    //         if (relativePath.StartsWith("/") )
    //         {
    //             yield return new InMemoryFileInfo(fileName);
    //         }
    //     }
    // }
    public override DirectoryInfoBase? GetDirectory(string path)
    {
        if (path.StartsWith(_directoryName))
        {
            return new InMemoryDirectoryInfo(path);
        }
        return null;
    }

    public override FileInfoBase? GetFile(string path)
    {
        if (path.StartsWith(_directoryName))
        {
            return new InMemoryFileInfo(path);
        }
        return null;
    }

    public override string Name => _directoryName;
    public override DirectoryInfoBase? ParentDirectory => null;
    public override string FullName => _directoryName;
}
