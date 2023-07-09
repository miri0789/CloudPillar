namespace CloudPillar.Agent.Handlers;


public class FileStreamerHandler : IFileStreamerHandler
{
    public async Task WriteChunkToFileAsync(string filePath, long writePosition, byte[] bytes)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            fileStream.Seek(writePosition, SeekOrigin.Begin);
            await fileStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
           File.Delete(filePath);
        }
    }

    public async Task<bool> CheckFileBytesNotEmptyAsync(string filePath, long startPosition, long endPosition)
    {
        if (startPosition > endPosition)
        {
            return true;
        }

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fileStream.Seek(startPosition, SeekOrigin.Begin);
            byte[] buffer = new byte[endPosition - startPosition + 1];
            await fileStream.ReadAsync(buffer, 0, buffer.Length);
            return Array.IndexOf(buffer, (byte)0) == -1;
        }

        return true;
    }

    public async Task DeleteFileBytesAsync(string filePath, long startPosition, long endPosition)
    {
        if (startPosition > endPosition)
        {
            return;
        }

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
        {
            fileStream.Seek(startPosition, SeekOrigin.Begin);

            byte[] buffer = new byte[endPosition - startPosition + 1];
            await fileStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
