using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.Threading;

namespace Frends.Community.Azure.Blob.Tests
{
    /// <summary>
    /// Note, you need Azure Storage Emulator 5.2 up and running to run these tests
    /// https://azure.microsoft.com/en-us/downloads/
    /// </summary>
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
        [ExpectedException(typeof(ArgumentException), "Should have thrown exception with nonexisting file")]
        public async Task UploadFile_ShouldThrowArgumentExceptionIfFileWasNotFound()
        {
            await Upload.UploadFileAsync(
                new UploadInput { SourceFile = "NonExistingFile" },
                new UploadOptions(),
                new CancellationToken());
        }

        [TestMethod]
        public async Task UploadFile_ShouldUploadFileAsBlockBlob()
        {
            var input = new UploadInput
            {
                SourceFile = _testFilePath
            };
            var options = new UploadOptions
            {
                ContainerName = _containerName,
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = _connectionString,
                Overwrite = true
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);

            string uriResult = await Upload.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = Utils.GetCloudBlob(container, "TestFile.xml", AzureBlobType.Block);

            StringAssert.EndsWith(uriResult, "test-container/TestFile.xml");
            Assert.IsTrue(blobResult.Exists(), "Uploaded TestFile.xml blob should exist");
        }

        [TestMethod]
        public async Task UploadFile_ShouldRenameFileToBlob()
        {
            var input = new UploadInput
            {
                SourceFile = _testFilePath
            };
            var options = new UploadOptions
            {
                RenameTo = "RenamedFile.xml",
                ContainerName = "test-container",
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = "UseDevelopmentStorage=true",
                Overwrite = true
            };

            string result = await Upload.UploadFileAsync(input, options, new CancellationToken());

            StringAssert.EndsWith(result, "test-container/RenamedFile.xml");
        }
    }
}
