using blobstreamer.Models;
using blobstreamer.Services;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Moq;
using NUnit.Framework;
using System.Reflection;
using System.Threading.Tasks;

namespace YourNamespace.Tests
{

    public interface ICloudBlobWrapper
    {
        Task FetchAttributesAsync();
        BlobProperties Properties { get; }
    }

    [TestFixture]
    public class BlobTestFixture
    {
        private Mock<CloudBlobContainer> _mockContainer;
        private Mock<ServiceClient> _mockServiceClient;
        private Mock<CloudBlockBlob> _mockBlockBlob;
        private IBlobService _blobService;

        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable("STORAGE_CONNECTION_STRING", "DefaultEndpointsProtocol=https;AccountName=szlaaa12026ce;AccountKey=ZCspYG/5HjJTVdY+9Hf/ZkzHHspd2BGXH8cVPUkgDzmvA5JCyRWk5B6aFG1izNq7YC+i/VsDrxOb+AStciDLcw==;EndpointSuffix=core.windows.net");
            Environment.SetEnvironmentVariable("BLOB_CONTAINER_NAME", "iot-firmware");
            Environment.SetEnvironmentVariable("IOTHUB_CONNECTION_STRING", "HostName=szlabs-iot-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dMBNypodzUSWPbxTXdWaV4PxJTR3jCwehPFCQn+XJXc=");


            _mockContainer = new Mock<CloudBlobContainer>(new Uri("http://storageaccount/container"));
            _mockServiceClient = new Mock<ServiceClient>();

            _mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://storageaccount/container/blob"));

            _blobService = new BlobService();

            var containerField = typeof(BlobService).GetField("_container", BindingFlags.NonPublic | BindingFlags.Instance);
            containerField.SetValue(_blobService, _mockContainer.Object);

            var serviceClientField = typeof(BlobService).GetField("_serviceClient", BindingFlags.NonPublic | BindingFlags.Instance);
            serviceClientField.SetValue(_blobService, _mockServiceClient.Object);
        }


        [Test]
        public async Task SendRangeByChunksAsync_ShouldSendBlobMessages()
        {
            string deviceId = "test-device";
            string fileName = "test-file.txt";
            int chunkSize = 1024;
            int rangeSize = 4096;
            int rangeIndex = 0;
            long startPosition = 0;
            _mockContainer.Setup(c => c.GetBlockBlobReference(fileName)).Returns(_mockBlockBlob.Object);
            _mockBlockBlob.Setup(b => b.DownloadRangeToByteArrayAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>()));

            _mockServiceClient.Setup(s => s.SendAsync(deviceId, It.IsAny<Message>(), null)).Returns(Task.CompletedTask);
            await _blobService.SendRangeByChunksAsync(deviceId, fileName, chunkSize, rangeSize, rangeIndex, startPosition);
            _mockServiceClient.Verify(s => s.SendAsync(deviceId, It.IsAny<Message>(), null), Times.Exactly(5));
        }

        [Test]
        public async Task SendStartBlobMessage_ShouldSendStartBlobMessage()
        {
            string deviceId = "test-device";
            string fileName = "test-file.txt";
            long blobLength = 1024;
            _mockServiceClient.Setup(s => s.SendAsync(deviceId, It.IsAny<Message>(), null));
            await _blobService.SendStartBlobMessage(deviceId, fileName, blobLength);
            _mockServiceClient.Verify(s => s.SendAsync(deviceId, It.IsAny<Message>(), null), Times.Exactly(1));
        }

        [Test]
        public async Task GetBlobMetadataAsync_ShouldReturnBlobProperties()
        {
            string fileName = "test-file.txt";
            _mockContainer.Setup(c => c.GetBlockBlobReference(fileName)).Returns(_mockBlockBlob.Object);
            _mockBlockBlob.Setup(b => b.FetchAttributesAsync()).Returns(Task.CompletedTask);
            BlobProperties expectedProperties = new BlobProperties();
            var result = await _blobService.GetBlobMetadataAsync(fileName);
            _mockBlockBlob.Verify(b => b.FetchAttributesAsync(), Times.Once);
        }
        [Test]
        public async Task GetBlobMetadataAsync_FileNotExists_ReturnsNull()
        {
            string fileName = "nonexistent-file.txt";
            _mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("https://your-storage-account.blob.core.windows.net/container-name/blob-name"));
            _mockBlockBlob.Setup(b => b.FetchAttributesAsync()).ThrowsAsync(new StorageException("The specified blob does not exist.", null));
            _mockContainer.Setup(c => c.GetBlockBlobReference(fileName)).Returns(_mockBlockBlob.Object);

            async Task SendRequest() => await _blobService.GetBlobMetadataAsync(fileName);
            Assert.ThrowsAsync<StorageException>(SendRequest);
        }


        [Test]
        public async Task SendMessage_ShouldRetryAndSucceed()
        {
            string deviceId = "test-device";
            string fileName = "test-file.txt";
            long blobLength = 1024;
            Environment.SetEnvironmentVariable("RETRY_POLICY_EXPONENT", "3");

            _mockServiceClient
                .SetupSequence(s => s.SendAsync(deviceId, It.IsAny<Message>(), null))
                .Throws(new Exception("First send failed"))
                .Throws(new Exception("Second send failed"))
                .Returns(Task.CompletedTask);

            await _blobService.SendStartBlobMessage(deviceId, fileName, blobLength);
            _mockServiceClient.Verify(s => s.SendAsync(deviceId, It.IsAny<Message>(), null), Times.Exactly(3));
        }
    }
}
