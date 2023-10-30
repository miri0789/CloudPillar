using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

public class GitIgnoreMatcher
{
    private readonly Matcher matcher;

    public GitIgnoreMatcher(string rootPath, string[] patterns)
    {
        matcher = new Matcher();
        foreach (var pattern in patterns)
        {
            matcher.AddInclude(pattern);
        }
        matcher.AddInclude("**"); // Exclude everything by default
        matcher.AddExclude(""); // Include nothing by default
        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(rootPath)));
    }

    public bool IsMatch(string filePath)
    {
        var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(filePath));
        var directoryInfoWrapper = new DirectoryInfoWrapper(directoryInfo);

        var result = matcher.Execute(directoryInfoWrapper);
        Console.WriteLine("Matched files:");
        foreach (var match in result.Files)
        {
            Console.WriteLine(Path.Combine(directoryInfo.FullName, match.Path));
        }

        var fileInfo = new FileInfo(filePath);
        foreach (var match in result.Files)
        {
            if (Path.Combine(directoryInfo.FullName, match.Path) == fileInfo.FullName)
            {
                return true;
            }
        }
        return false;
    }
}