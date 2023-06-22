namespace iotdevice.Services;

public interface IFileStreamerService
{

}

public class FileStreamerService : IFileStreamerService
{

    private async Task<long> WriteChunkToFile(string filename, int writePosition, byte[] bytes, Stopwatch stopwatch, long writtenAmount = -1, int progressPercent = 0)
    {
        if (writtenAmount < 0) writtenAmount = writePosition;
        long totalBytesDownloaded = writtenAmount + bytes.Length;
        string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
        if (!stopwatch.IsRunning)
        {
            stopwatch.Start();
            totalBytesDownloaded = bytes.Length;
        }
        using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
        {
            fileStream.Seek(writePosition, SeekOrigin.Begin);
            await fileStream.WriteAsync(bytes, 0, bytes.Length);
        }
        double timeElapsedInSeconds = stopwatch.Elapsed.TotalSeconds;
        double throughput = totalBytesDownloaded / timeElapsedInSeconds / 1024.0; // in KiB/s

        TwinAction? action = _downloadAction;
        action?.ReportProgress(progressPercent);

        _progressObserver?.ReportProgress(filename, progressPercent, false);

        Console.WriteLine($"%{progressPercent:00} @pos: {writePosition:00000000000} tot: {writtenAmount:00000000000} Throughput: {throughput:0.00} KiB/s");
        if (progressPercent == 100)
        {
            if (action != null)
            {
                action.ReportSuccess("FinishedTransit", "Finished streaming as the last chunk arrived.");
                await action.Persist();
            }
            Console.WriteLine($"{DateTime.Now}: Finished streaming as the last chunk arrived.");
        }
        return totalBytesDownloaded;
    }

}
