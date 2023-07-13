using System.Text;
using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Interfaces;
using System.Reflection;

namespace CloudPillar.Agent.Tests;

public class FileStreamerTestFixture
{
    private IFileStreamerFactory _FileStreamerFactory;
    private string _filePath;
    private string _fileDirectory;

    public FileStreamerTestFixture() {        
        _FileStreamerFactory = new FileStreamerFactory();
        _fileDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _filePath = Path.Combine(_fileDirectory, "testfile.txt");
    }

    [SetUp]
    public void Setup()
    {
        File.Create(_filePath).Dispose();
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

        await _FileStreamerFactory.WriteChunkToFileAsync(_filePath, writePosition, expectedBytes);

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

        await _FileStreamerFactory.WriteChunkToFileAsync(_filePath, writePosition, newBytes);

        byte[] actualBytes = await File.ReadAllBytesAsync(_filePath);
        CollectionAssert.AreEqual(expectedBytes, actualBytes);
    }

    [Test]
    public async Task DeleteFile_OnValidFilePath_Delete()
    {
        _FileStreamerFactory.DeleteFile(_filePath);
        Assert.IsFalse(File.Exists(_filePath), "File should be deleted.");
    }


    [Test]
    public async Task CheckFileBytesNotEmpty_OnValidRange_ReturnTrue()
    {
        byte[] bytes = { 65, 66, 67, 68, 69, 70 };
        await File.WriteAllBytesAsync(_filePath, bytes);

        long startPosition = 2;
        long endPosition = 4;
        bool result = await _FileStreamerFactory.HasBytesAsync(_filePath, startPosition, endPosition);
        Assert.IsTrue(result, "Expected the file bytes to be not empty within the range.");
    }

    [Test]
    public async Task CheckFileBytesNotEmpty_InValidRange_ReturnTrue()
    {
        long startPosition = 100;
        long endPosition = 0;
        bool result = await _FileStreamerFactory.HasBytesAsync(_filePath, startPosition, endPosition);
        Assert.IsTrue(result, "Expected the function to return false for an invalid range.");
    }

    [Test]
    public async Task CheckFileBytesNotEmpty_EmptyRange_ReturnFalse()
    {
        byte[] bytes = { 65, 66, 0, 68, 69, 70 };
        await File.WriteAllBytesAsync(_filePath, bytes);

        long startPosition = 2;
        long endPosition = 4;
        bool result = await _FileStreamerFactory.HasBytesAsync(_filePath, startPosition, endPosition);
        Assert.IsFalse(result, "Expected the function to return false when empty bytes are found within the range.");
    }
}