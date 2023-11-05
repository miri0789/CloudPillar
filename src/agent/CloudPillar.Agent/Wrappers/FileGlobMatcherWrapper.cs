using Microsoft.Extensions.FileSystemGlobbing;

namespace CloudPillar.Agent.Wrappers;
public class FileGlobMatcherWrapper : IFileGlobMatcherWrapper
{

    public FileGlobMatcher CreateFileGlobMatcher(string[] patterns)
    {
        return new FileGlobMatcher(patterns);
    }
    public bool IsMatch(FileGlobMatcher matcher, string root, string fileName,string[] patterns)
    {
        if (matcher == null)
        {
            throw new ArgumentNullException("matcher is null");
        }
        return matcher.IsMatch(root, fileName,patterns);
    }
}
