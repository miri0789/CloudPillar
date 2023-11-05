using CloudPillar.Agent.Handlers;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

public class FileGlobMatcher
{
    private readonly Matcher matcher;
    public FileGlobMatcher()
    {

    }
    public FileGlobMatcher(string[] patterns)
    {
        matcher = new Matcher();
        foreach (var pattern in patterns)
        {
            matcher.AddInclude(pattern);
        }

    }

    public bool IsMatch(string rootPath, string filePath, string[] patterns)
    {
        var matcher = new Matcher();
        foreach (var pattern in patterns)
        {
            matcher.AddInclude(pattern);
        }

        var fileInfo = new FileInfo(filePath);
        var directoryInfo = new DirectoryInfo(rootPath);
        var directoryInfoWrapper = new DirectoryInfoWrapper(directoryInfo);
        var result = matcher.Execute(new InMemoryDirectoryInfo(new List<string>() { filePath }, rootPath));

        // var result = matcher.Execute(directoryInfoWrapper);
        var fileMatch = result.Files.Where(file => fileInfo.FullName == Path.Combine(directoryInfo.FullName, file.Path).Replace("/", "\\")).Count() > 0;
        return fileMatch;
    }
}