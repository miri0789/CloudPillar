namespace iotdevice.Services;

public interface IFileStreamerHandler
{
    Task WriteChunkToFile(string fileName, int writePosition, byte[] bytes, string? filePath = "");

    Task DeleteFile(string fileName, string? filePath);
}

public class FileStreamerHandler : IFileStreamerHandler
{
    public async Task WriteChunkToFile(string fileName, int writePosition, byte[] bytes, string? filePath = "")
    {
        filePath = filePath ?? Directory.GetCurrentDirectory();
        string path = Path.Combine(filePath, fileName);

        using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            fileStream.Seek(writePosition, SeekOrigin.Begin);
            await fileStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    public async Task DeleteFile(string fileName, string? filePath)
    {
        filePath = filePath ?? Directory.GetCurrentDirectory();
        string path = Path.Combine(filePath, fileName);
        
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
