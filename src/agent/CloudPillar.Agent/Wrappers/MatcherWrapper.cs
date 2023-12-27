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
}
