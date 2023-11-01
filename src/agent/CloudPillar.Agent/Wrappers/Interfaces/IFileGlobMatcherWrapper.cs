using Microsoft.Extensions.FileSystemGlobbing;

namespace CloudPillar.Agent.Wrappers;

public interface IFileGlobMatcherWrapper
{
    public FileGlobMatcher CreateFileGlobMatcher(string[] patterns);
    public bool IsMatch(FileGlobMatcher matcher, string root, string fileName);

}
