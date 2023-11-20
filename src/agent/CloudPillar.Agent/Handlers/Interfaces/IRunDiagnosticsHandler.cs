namespace CloudPillar.Agent.Handlers;
public interface IRunDiagnosticsHandler
{
    Task CreateFileAsync();
    Task UploadFileAsync(CancellationToken cancellationToken);
    Task<bool> CompareUploadAndDownloadFiles(string downloadFilePath);

}