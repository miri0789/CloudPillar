using CloudPillar.Agent.Wrappers.Interfaces;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CloudPillar.Agent.Wrappers.interfaces;

public class MatcherWrapper : IMatcherWrapper
{
    private const string SEPARATOR = "/";
    private const string DOUBLE_SEPARATOR = "\\";

    public PatternMatchingResult IsMatch(string[] patterns, string rootPath, string filePath)
    {
        Matcher matcher = new Matcher();

        matcher.AddIncludePatterns(patterns);
        return matcher.Match(rootPath.ToLower(), filePath.ToLower());
    }
    public bool DoesFileMatchPattern(PatternMatchingResult matchingResult, string rootPath, string filePath)
    {
        return matchingResult?.Files.Any(file => filePath.Replace(DOUBLE_SEPARATOR, SEPARATOR)?.ToLower() == Path.Combine(rootPath, file.Path).Replace(DOUBLE_SEPARATOR, SEPARATOR)?.ToLower()) ?? false;
    }

}
