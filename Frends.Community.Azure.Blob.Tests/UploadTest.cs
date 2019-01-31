using TestConfigurationHandler;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq.Expressions;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class UploadTest
    {
        /// <summary>
        /// Container name for tests
        /// </summary>
        private string _containerName;

        /// <summary>
        /// Connection string for Azure Storage Emulator
        /// </summary>
        private readonly string _connectionString = ConfigHandler.ReadConfigValue("HiQ.AzureBlobStorage.ConnString");

        /// <summary>
        /// Some random file for test purposes
        /// </summary>
        private string _testFilePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\TestFiles\TestFile.xml";
        /*
        [TestInitialize]
        public void TestSetup()
        {
            // Generate unique container name to avoid conflicts when running multiple tests
            _containerName = $"test-container{DateTime.Now.ToString("mmssffffff")}";
        }

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
                Overwrite = true,
                CreateContainerIfItDoesNotExist = true
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);

            var result = await UploadTask.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = Utils.GetCloudBlob(container, "TestFile.xml", AzureBlobType.Block);

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
                CreateContainerIfItDoesNotExist = true
            };

            var result = await UploadTask.UploadFileAsync(input, options, new CancellationToken());

            StringAssert.EndsWith(result.Uri, $"{_containerName}/RenamedFile.xml");
        }
        */
        [TestMethod]
        public async Task UploadFileAsync_NewShit()
        {
            var accName = "devstoreaccount1";
            
            var accKey =
                "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
        
            var containerName = "testcont";

            var connectionString = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1:10000/";
            
            var filepath = @"c:\testfile\TestFile.xml";

            var input = new UploadInput
            {
                SourceFile = filepath,
                Compress = false,
                ContentsOnly = true
            };

            var guid = Guid.NewGuid().ToString();

            var options = new DestinationProperties
            {
                ContainerName = containerName,
                BlobType = AzureBlobType.Block,
                ParallelOperations = 24,
                ConnectionString = connectionString,
                Overwrite = false,
                CreateContainerIfItDoesNotExist = true,
                ContentType = "text/xml",
                FileEncoding = "utf8",
                RenameTo = guid + ".gz"
            };
            var container = Utils.GetBlobContainer(connectionString, containerName);

            var result = await UploadTask.UploadFileAsync(input, options, new CancellationToken());
            var blobResult = Utils.GetCloudBlob(container, "TestFile.xml", AzureBlobType.Block);
            
            var info = new FileInfo(filepath);
            using(var stream = info.Open(FileMode.Open))
            StringAssert.EndsWith(result.Uri, $"{containerName}/{guid}.gz");
            Assert.IsTrue(blobResult.Exists(), "Uploaded TestFile.xml blob should exist");

        }
        
    }
}
