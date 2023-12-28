using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Wrappers;

public class DirectoryWrapper : IDirectoryWrapper
{
    public string[] GetFiles(string path, string searchPattern)
    {
        return Directory.GetFiles(path, searchPattern);
    }
}