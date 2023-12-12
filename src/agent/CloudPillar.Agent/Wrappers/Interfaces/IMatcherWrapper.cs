
using Microsoft.Extensions.FileSystemGlobbing;

namespace CloudPillar.Agent.Wrappers.Interfaces;

public interface IMatcherWrapper
{
    PatternMatchingResult IsMatch(string[] patterns, string rootPath, string filePath);
    bool DoesFileMatchPattern(PatternMatchingResult matchingResult, string rootPath, string filePath);
}