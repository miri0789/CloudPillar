namespace CloudPillar.Agent.Wrappers.Interfaces;

public interface IDirectoryWrapper
{
    string[] GetFiles(string path, string searchPattern);
}