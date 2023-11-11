

using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;
public class RunDiagnosticsHandler : IRunDiagnosticsHandler
{
    private const int FILE_SIZE_KB = 128;
    private const int FILE_SIZE_BYTES = FILE_SIZE_KB * 1024;
    private const string FILE_NAME = "diagnosticFile";
    private const string FILE_EXSTENSION = ".txt";
    private string basePath = AppDomain.CurrentDomain.BaseDirectory;
    private string destPath;

    private readonly IFileUploaderHandler _fileUploaderHandler;

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));

        destPath = Path.Combine(basePath, FILE_NAME + FILE_EXSTENSION);
    }

    public async Task CreateFileAsync()
    {
        var buffer = new byte[FILE_SIZE_BYTES];
        new Random().NextBytes(buffer);
        var text = System.Text.Encoding.Default.GetString(buffer);
        await File.WriteAllTextAsync(destPath, text);
    }

    public async Task UploadFileAsync(CancellationToken cancellationToken)
    {
        var uploadAction = new UploadAction()
        {
            Action = TwinActionType.SingularUpload,
            Description = "upload file by run diagnostic",
            ActionId = Guid.NewGuid().ToString(),
            Method = FileUploadMethod.Stream,
            FileName = destPath
        };

        var actionToReport = new ActionToReport();
        await _fileUploaderHandler.UploadFilesToBlobStorageAsync(destPath, uploadAction, actionToReport, cancellationToken);
    }


    public async Task DownloadFile(CancellationToken cancellationToken)
    {
    }
}