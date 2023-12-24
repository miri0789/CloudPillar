using CloudPillar.Agent.Wrappers.Interfaces;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CloudPillar.Agent.Wrappers;

public class MatcherWrapper : IMatcherWrapper
{

    public PatternMatchingResult IsMatch(string[] patterns, string rootPath, string filePath)
    {
        Matcher matcher = new Matcher();

        patterns = patterns.Select(s => s.ToLower()).ToArray();
        matcher.AddIncludePatterns(patterns);
        return matcher.Match(rootPath.ToLower(), filePath.ToLower());
    }
    public bool DoesFileMatchPattern(PatternMatchingResult matchingResult, string rootPath, string filePath)
    {
        return matchingResult?.Files.Any(file => filePath.Replace(FileConstants.DOUBLE_SEPARATOR, FileConstants.SEPARATOR)
        .Replace(FileConstants.DOUBLE_FORWARD_SLASH_SEPARATOR, FileConstants.SEPARATOR)?.ToLower() ==
        Path.Combine(rootPath, file.Path).Replace(FileConstants.DOUBLE_SEPARATOR, FileConstants.SEPARATOR)
        .Replace(FileConstants.DOUBLE_FORWARD_SLASH_SEPARATOR, FileConstants.SEPARATOR)?.ToLower()) ?? false;
    }

}
