using Backend.Keyholder.Wrappers.Interfaces;

namespace Backend.Keyholder.Wrappers;

public class FileStreamerWrapper : IFileStreamerWrapper
{

    public async Task<string> ReadAllTextAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }

    public FileStream OpenRead(string filePath)
    {
        return File.OpenRead(filePath);
    }
}
