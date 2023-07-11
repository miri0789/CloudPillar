using System.Text;
using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Interfaces;
using System.Reflection;

namespace CloudPillar.Agent.Tests;

public class FileStreamerTestFixture
{
    private IFileStreamerHandler _fileStreamerHandler;
    private string _filePath;
    private string _fileDirectory;

    [SetUp]
    public void Setup()
    {
        _fileStreamerHandler = new FileStreamerHandler();
        _fileDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _filePath = Path.Combine(_fileDirectory, "testfile.txt");
    }

    [TearDown]
    public void Cleanup()
    {
        File.Delete(_filePath);
    }
    
    [Test]
    public async Task WriteChunkToFile_OnExistingValidFile_AppendBytes()
    {
        int writePosition = 0;
        byte[] expectedBytes = { 65, 66, 67 };

        await _fileStreamerHandler.WriteChunkToFileAsync(_filePath, writePosition, expectedBytes);

        byte[] actualBytes = await File.ReadAllBytesAsync(_filePath);
        CollectionAssert.AreEqual(expectedBytes, actualBytes);
    }


    [Test]
    public async Task WriteChunkToFile_OnExistingValidFullFile_AppendBytes()
    {
        int writePosition = 3;
        byte[] existingBytes = { 65, 66, 67 };
        byte[] newBytes = { 68, 69, 70 };
        byte[] expectedBytes = { 65, 66, 67, 68, 69, 70 };

        await File.WriteAllBytesAsync(_filePath, existingBytes);

        await _fileStreamerHandler.WriteChunkToFileAsync(_filePath, writePosition, newBytes);

        byte[] actualBytes = await File.ReadAllBytesAsync(_filePath);
        CollectionAssert.AreEqual(expectedBytes, actualBytes);
    }

    [Test]
    public async Task DeleteFile_OnValidFilePath_Delete()
    {
        string fileName = "testDeletd.txt";
        string fullPath = Path.Combine(_fileDirectory, fileName);
        File.Create(fullPath).Dispose();
        _fileStreamerHandler.DeleteFile(fullPath);
        Assert.IsFalse(File.Exists(fullPath), "File should be deleted.");
    }

    [Test]
    public async Task DeleteFile_OnNotExistFilePath_NotDelete()
    {
        string fileName = "nonexistent.txt";
        string filePath = Path.Combine(_fileDirectory, fileName);
        string fullPath = Path.Combine(filePath, fileName);
        _fileStreamerHandler.DeleteFile(filePath);
        Assert.Pass("No exception should be thrown when deleting a non-existent file.");
    }


    [Test]
    public async Task CheckFileBytesNotEmpty_OnValidRange_ReturnTrue()
    {
        byte[] bytes = { 65, 66, 67, 68, 69, 70 };
        await File.WriteAllBytesAsync(_filePath, bytes);

        long startPosition = 2;
        long endPosition = 4;
        bool result = await _fileStreamerHandler.HasBytesAsync(_filePath, startPosition, endPosition);
        Assert.IsTrue(result, "Expected the file bytes to be not empty within the range.");
    }

    [Test]
    public async Task CheckFileBytesNotEmpty_InValidRange_ReturnTrue()
    {
        long startPosition = 100;
        long endPosition = 0;
        bool result = await _fileStreamerHandler.HasBytesAsync(_filePath, startPosition, endPosition);
        Assert.IsTrue(result, "Expected the function to return false for an invalid range.");
    }

    [Test]
    public async Task CheckFileBytesNotEmpty_EmptyRange_ReturnFalse()
    {
        byte[] bytes = { 65, 66, 0, 68, 69, 70 };
        await File.WriteAllBytesAsync(_filePath, bytes);

        long startPosition = 2;
        long endPosition = 4;
        bool result = await _fileStreamerHandler.HasBytesAsync(_filePath, startPosition, endPosition);
        Assert.IsFalse(result, "Expected the function to return false when empty bytes are found within the range.");
    }

    [Test]
    public async Task DeleteFileBytes_ValidRange_FileBytesDeleted()
    {
        byte[] bytes = { 65, 66, 66, 68, 69, 70 };

        await File.WriteAllBytesAsync(_filePath, bytes);
        long startPosition = 1;
        long endPosition = 3;
        await _fileStreamerHandler.DeleteFileBytesAsync(_filePath, startPosition, endPosition);

        byte[] modifiedFileBytes = File.ReadAllBytes(_filePath);
        Assert.IsTrue(Array.FindAll(modifiedFileBytes, byteValue => byteValue == 0).Count() == 3, "Expected all file bytes within the range to be deleted.");
    }

    [Test]
    public async Task DeleteFileBytes_InvalidRange_NoChanges()
    {
        long startPosition = 100;
        long endPosition = 0;

        byte[] originalFileBytes = { 65, 66, 66, 68, 69, 70 };
        await File.WriteAllBytesAsync(_filePath, originalFileBytes);

        await _fileStreamerHandler.DeleteFileBytesAsync(_filePath, startPosition, endPosition);

        byte[] modifiedFileBytes = File.ReadAllBytes(_filePath);
        Assert.AreEqual(originalFileBytes.Length, modifiedFileBytes.Length, "Expected the file length to remain unchanged.");
        Assert.AreEqual(originalFileBytes, modifiedFileBytes, "Expected the file content to remain unchanged.");
    }
}