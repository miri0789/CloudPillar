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
        Matcher matcher = new Matcher();

        matcher.AddIncludePatterns(patterns);
        var result = matcher.Match(rootPath, filePath);

        var fileMatch = result.Files.Where(file => filePath.Replace("\\","/") == Path.Combine(rootPath,file.Path).Replace("\\","/")).Count() > 0;
        return fileMatch;
    }
}