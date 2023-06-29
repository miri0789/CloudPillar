using System.Text;
using Moq;
using CloudPillar.Agent.Handlers;

namespace CloudPillar.Agent.Tests;

public class FileStreamerTestFixture
{
    private IFileStreamerHandler _fileStreamerHandler;
    private string testDirectory;

    [SetUp]
    public void Setup()
    {
        _fileStreamerHandler = new FileStreamerHandler();
        testDirectory = Path.Combine(Path.GetTempPath(), "TestDirectory");
        Directory.CreateDirectory(testDirectory);
    }

    [TearDown]
    public void Cleanup()
    {
        Directory.Delete(testDirectory, true);
    }
    [Test]
    public async Task WriteChunkToFile_Should_WriteBytesToFile()
    {
        string fileName = "testfile.txt";
        int writePosition = 0;
        byte[] bytes = { 65, 66, 67 };

        await _fileStreamerHandler.WriteChunkToFile(fileName, writePosition, bytes, testDirectory);

        string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        byte[] writtenBytes = await File.ReadAllBytesAsync(filePath);
        CollectionAssert.AreEqual(bytes, writtenBytes);
    }


    [Test]
    public async Task WriteChunkToFile_Should_AppendBytesToExistingFile()
    {
        string fileName = "existingfile.txt";
        int writePosition = 3;
        byte[] existingBytes = { 65, 66, 67 };
        byte[] newBytes = { 68, 69, 70 };
        byte[] expectedBytes = { 65, 66, 67, 68, 69, 70 };

        string filePath = Path.Combine(testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, existingBytes);

        await _fileStreamerHandler.WriteChunkToFile(fileName, writePosition, newBytes, testDirectory);

        byte[] writtenBytes = await File.ReadAllBytesAsync(filePath);
        CollectionAssert.AreEqual(expectedBytes, writtenBytes);
    }

    [Test]
    public async Task DeleteFile_WithValidFilePath_DeletesFile()
    {
        string fileName = "test.txt";
        string filePath = testDirectory;
        string fullPath = Path.Combine(filePath, fileName);
        File.Create(fullPath).Dispose();
        await _fileStreamerHandler.DeleteFile(fileName, filePath);
        Assert.IsFalse(File.Exists(fullPath), "File should be deleted.");
    }

    [Test]
    public async Task DeleteFile_WithNullFilePath_DeletesFileInCurrentDirectory()
    {
        string fileName = "test.txt";
        string currentDirectory = Directory.GetCurrentDirectory();
        string fullPath = Path.Combine(currentDirectory, fileName);
        File.Create(fullPath).Dispose();
        await _fileStreamerHandler.DeleteFile(fileName, null);
        Assert.IsFalse(File.Exists(fullPath), "File should be deleted.");
    }

    [Test]
    public async Task DeleteFile_FileDoesNotExist_NoExceptionThrown()
    {
        string fileName = "nonexistent.txt";
        string filePath = testDirectory;
        string fullPath = Path.Combine(filePath, fileName);
        await _fileStreamerHandler.DeleteFile(fileName, filePath);
        Assert.Pass("No exception should be thrown when deleting a non-existent file.");
    }


    [Test]
    public async Task CheckFileBytesNotEmpty_ValidRange_NoEmptyBytes()
    {
        string fileName = "test.txt";
        byte[] bytes = { 65, 66, 67, 68, 69, 70 };

        string filePath = Path.Combine(testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        long startPosition = 2;
        long endPosition = 4;
        bool result = await _fileStreamerHandler.CheckFileBytesNotEmpty(filePath, startPosition, endPosition);
        Assert.IsTrue(result, "Expected the file bytes to be not empty within the range.");
        await _fileStreamerHandler.DeleteFile(fileName, filePath);
    }

    [Test]
    public async Task CheckFileBytesNotEmpty_InvalidRange_ReturnsTrue()
    {
        string fileName = "test.txt";
        string filePath = Path.Combine(testDirectory, fileName);
        long startPosition = 100;
        long endPosition = 0;
        bool result = await _fileStreamerHandler.CheckFileBytesNotEmpty(filePath, startPosition, endPosition);
        Assert.IsTrue(result, "Expected the function to return false for an invalid range.");
    }

    [Test]
    public async Task CheckFileBytesNotEmpty_EmptyBytesFound_ReturnsFalse()
    {
        string fileName = "test.txt";
        byte[] bytes = { 65, 66, 0, 68, 69, 70 };

        string filePath = Path.Combine(testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        long startPosition = 2;
        long endPosition = 4;
        bool result = await _fileStreamerHandler.CheckFileBytesNotEmpty(filePath, startPosition, endPosition);
        Assert.IsFalse(result, "Expected the function to return false when empty bytes are found within the range.");
        await _fileStreamerHandler.DeleteFile(fileName, filePath);
    }

    [Test]
    public async Task DeleteFileBytes_ValidRange_FileBytesDeleted()
    {
        string fileName = "test.txt";
        byte[] bytes = { 65, 66, 66, 68, 69, 70 };

        string filePath = Path.Combine(testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, bytes);
        long startPosition = 1;
        long endPosition = 3;
        await _fileStreamerHandler.DeleteFileBytes(filePath, startPosition, endPosition);

        byte[] modifiedFileBytes = File.ReadAllBytes(filePath);
        Assert.IsTrue(Array.FindAll(modifiedFileBytes, byteValue => byteValue == 0).Count() == 3, "Expected all file bytes within the range to be deleted.");
        await _fileStreamerHandler.DeleteFile(fileName, filePath);
    }

    [Test]
    public async Task DeleteFileBytes_InvalidRange_NoChanges()
    {
        string fileName = "test.txt";
        string filePath = Path.Combine(testDirectory, fileName);
        long startPosition = 100;
        long endPosition = 0;

        byte[] originalFileBytes = { 65, 66, 66, 68, 69, 70 };
        await File.WriteAllBytesAsync(filePath, originalFileBytes);

        await _fileStreamerHandler.DeleteFileBytes(filePath, startPosition, endPosition);

        byte[] modifiedFileBytes = File.ReadAllBytes(filePath);
        Assert.AreEqual(originalFileBytes.Length, modifiedFileBytes.Length, "Expected the file length to remain unchanged.");
        Assert.AreEqual(originalFileBytes, modifiedFileBytes, "Expected the file content to remain unchanged.");
        await _fileStreamerHandler.DeleteFile(fileName, filePath);
    }
}