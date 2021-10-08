using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class UploadTest
    {
        /// <summary>
        ///     Connection string for Azure Storage Emulator
        /// </summary>
        private readonly string _connectionString = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ConnString");

        /// <summary>
        ///     Container name for tests
        /// </summary>
        private string _containerName;

        /// <summary>
        ///     Some random file for test purposes
        /// </summary>
        private readonly string _testFilePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\TestFiles\TestFile.xml";

        [TestInitialize]
        public void TestSetup()
        {
            // Generate unique container name to avoid conflicts when running multiple tests
            _containerName = $"test-container{DateTime.Now.ToString("mmssffffff", CultureInfo.InvariantCulture)}";
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            // delete whole container after running tests
            var container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.DeleteIfExistsAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UploadFileAsync_ShouldThrowArgumentExceptionIfFileWasNotFound()
        {
            await UploadTask.UploadFileAsync(
                new UploadInput {SourceFile = "NonExistingFile"},
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
                Overwrite = true,
                CreateContainerIfItDoesNotExist = true,
                FileEncoding = "utf-8"
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);

            var result = await UploadTask.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = container.GetBlobClient("TestFile.xml");

            StringAssert.EndsWith(result.Uri, $"{_containerName}/TestFile.xml");
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
                ContainerName = _containerName,
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = _connectionString,
                Overwrite = true,
                CreateContainerIfItDoesNotExist = true,
                FileEncoding = "utf-8"
            };

            var result = await UploadTask.UploadFileAsync(input, options, new CancellationToken());

            StringAssert.EndsWith(result.Uri, $"{_containerName}/RenamedFile.xml");
        }

        [TestMethod]
        public async Task UploadFileAsync_ShouldUploadCompressedFile()
        {
            var input = new UploadInput
            {
                SourceFile = _testFilePath,
                Compress = true,
                ContentsOnly = true
            };

            var guid = Guid.NewGuid().ToString();
            var renameTo = guid + ".gz";

            var options = new DestinationProperties
            {
                ContainerName = _containerName,
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = _connectionString,
                Overwrite = false,
                CreateContainerIfItDoesNotExist = true,
                ContentType = "text/xml",
                FileEncoding = "utf8",
                RenameTo = renameTo
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);

            await UploadTask.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = container.GetBlobClient(renameTo);

            Assert.IsTrue(blobResult.Exists(), "Uploaded TestFile.xml blob should exist");
        }

        [TestMethod]
        public async Task UploadFileAsync_ContentTypeIsForcedProperly()
        {
            var input = new UploadInput
            {
                SourceFile = _testFilePath,
                Compress = false,
                ContentsOnly = false
            };

            var guid = Guid.NewGuid().ToString();
            var renameTo = guid + ".gz";

            var options = new DestinationProperties
            {
                ContainerName = _containerName,
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = _connectionString,
                Overwrite = false,
                CreateContainerIfItDoesNotExist = true,
                ContentType = "foo/bar",
                FileEncoding = "utf8",
                RenameTo = renameTo
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);

            await UploadTask.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = container.GetBlobClient(renameTo);
            var properties = await blobResult.GetPropertiesAsync(null, new CancellationToken());

            Assert.IsTrue(properties.Value.ContentType == "foo/bar");
        }

        [TestMethod]
        public async Task UploadFileAsync_ContentEncodingIsGzipWhenCompressed()
        {
            var input = new UploadInput
            {
                SourceFile = _testFilePath,
                Compress = true,
                ContentsOnly = true
            };

            var guid = Guid.NewGuid().ToString();
            var renameTo = guid + ".gz";

            var options = new DestinationProperties
            {
                ContainerName = _containerName,
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = _connectionString,
                Overwrite = false,
                CreateContainerIfItDoesNotExist = true,
                ContentType = "foo/bar",
                FileEncoding = "utf8",
                RenameTo = renameTo
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);

            await UploadTask.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = container.GetBlobClient(renameTo);
            var properties = await blobResult.GetPropertiesAsync(null, new CancellationToken());

            Assert.IsTrue(properties.Value.ContentEncoding == "gzip");
        }
    }
}