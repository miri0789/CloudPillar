namespace Backend.Keyholder.Wrappers.Interfaces;

public interface IFileStreamerWrapper
{
    Task<string> ReadAllTextAsync(string filePath);
    FileStream OpenRead(string filePath);
}
