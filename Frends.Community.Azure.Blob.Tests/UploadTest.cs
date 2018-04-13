using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class UploadTest
    {
        /// <summary>
        /// Container name for tests
        /// </summary>
        private readonly string _containerName = "test-container";

        /// <summary>
        /// Connection string for Azure Storage Emulator
        /// </summary>
        private readonly string _connectionString = "UseDevelopmentStorage=true";

        /// <summary>
        /// Some random file for test purposes
        /// </summary>
        private string _testFilePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\TestFiles\TestFile.xml";

        [TestCleanup]
        public async Task Cleanup()
        {
            // delete whole container after running tests
            CloudBlobContainer container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.DeleteIfExistsAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UploadFileAsync_ShouldThrowArgumentExceptionIfFileWasNotFound()
        {
            var result = await UploadTask.UploadFileAsync(
                new UploadInput { SourceFile = "NonExistingFile" },
                new DestinationProperties(),
                new CancellationToken());
        }

        [TestMethod]
        public async Task UploadFileAsync_ShouldUploadFileAsBlockBlob()
        {
            var input = new UploadInput
            {
                SourceFile = _testFilePath
            };
            var options = new DestinationProperties
            {
                ContainerName = _containerName,
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = _connectionString,
                Overwrite = true
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);

            var result = await UploadTask.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = Utils.GetCloudBlob(container, "TestFile.xml", AzureBlobType.Block);

            StringAssert.EndsWith("test-container/TestFile.xml", result.Uri);
            Assert.IsTrue(blobResult.Exists(), "Uploaded TestFile.xml blob should exist");
        }

        [TestMethod]
        public async Task UploadFileAsync_ShouldRenameFileToBlob()
        {
            var input = new UploadInput
            {
                SourceFile = _testFilePath
            };
            var options = new DestinationProperties
            {
                RenameTo = "RenamedFile.xml",
                ContainerName = "test-container",
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = "UseDevelopmentStorage=true",
                Overwrite = true
            };

            var result = await UploadTask.UploadFileAsync(input, options, new CancellationToken());

            StringAssert.EndsWith("test-container/RenamedFile.xml", result.Uri);
        }
    }
}
