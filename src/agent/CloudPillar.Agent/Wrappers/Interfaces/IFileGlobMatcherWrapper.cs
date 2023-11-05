using Microsoft.Extensions.FileSystemGlobbing;

namespace CloudPillar.Agent.Wrappers;

public interface IFileGlobMatcherWrapper
{
    FileGlobMatcher CreateFileGlobMatcher(string[] patterns);
    bool IsMatch(FileGlobMatcher matcher, string root, string fileName, string[] patterns);

}
