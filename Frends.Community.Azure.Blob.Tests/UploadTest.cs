using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class UploadTest
    {
        /// <summary>
        ///     Connection string for Azure Storage Account
        /// </summary>
        private readonly string _connectionString = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ConnString");
        private readonly string _appID = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_AppID");
        private readonly string _tenantID = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_TenantID");
        private readonly string _clientSecret = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ClientSecret");
        private readonly string _storageAccount = "testsorage01";

        /// <summary>
        ///     Container name for tests
        /// </summary>
        private string _containerName;

        /// <summary>
        ///     Some random file for test purposes
        /// </summary>
        private readonly string _testFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "TestFile.xml");

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
                ConnectionMethod = ConnectionMethod.ConnectionString,
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
        public async Task UploadFileAsync_UploadBlockBlobWithTag()
        {
            var Tags = new Tag[]
            {
                new Tag { Name = "TagName", Value = "TagValue" }
            };
            var input = new UploadInput
            {
                SourceFile = _testFilePath,
                Tags = Tags
            };
            var options = new DestinationProperties
            {
                ConnectionMethod = ConnectionMethod.ConnectionString,
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
                ConnectionMethod = ConnectionMethod.ConnectionString,
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
                ConnectionMethod = ConnectionMethod.ConnectionString,
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
                ConnectionMethod = ConnectionMethod.ConnectionString,
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
                ConnectionMethod = ConnectionMethod.ConnectionString,
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

        [TestMethod]
        public async Task UploadFileAsync_AccessTokenAuthenticationTest()
        {
            var input = new UploadInput
            {
                SourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "TestFile2.xml")
            };
            var oauth = new OAuthConnection
            {
                ApplicationID = _appID,
                TenantID = _tenantID,
                ClientSecret = _clientSecret,
                StorageAccountName = _storageAccount
            };
            var options = new DestinationProperties
            {
                ConnectionMethod = ConnectionMethod.OAuth2,
                Connection = oauth,
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
            var blobResult = container.GetBlobClient("TestFile2.xml");

            StringAssert.EndsWith(result.Uri, $"{_containerName}/TestFile2.xml");
            Assert.IsTrue(blobResult.Exists(), "Uploaded TestFile2.xml blob should exist");
        }
    }
}