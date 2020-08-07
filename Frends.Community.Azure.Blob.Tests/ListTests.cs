using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class ListTests
    {
        /// <summary>
        ///     Connection string for Azure Storage Emulator
        /// </summary>
        private readonly string _connectionString = Environment.GetEnvironmentVariable("HiQ.AzureBlobStorage.ConnString", EnvironmentVariableTarget.User);

        /// <summary>
        ///     Test blob name
        /// </summary>
        private readonly string _testBlob = "test-blob.txt";

        /// <summary>
        ///     Container name for tests
        /// </summary>
        private string _containerName;

        private ListSourceProperties _sourceProperties;

        /// <summary>
        ///     Some random file for test purposes
        /// </summary>
        private readonly string _testFilePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\TestFiles\TestFile.xml";

        [TestInitialize]
        public async Task TestSetup()
        {
            // Generate unique container name to avoid conflicts when running multiple tests
            _containerName = $"test-container{DateTime.Now.ToString("mmssffffff", CultureInfo.InvariantCulture)}";

            _sourceProperties = new ListSourceProperties
            {
                ConnectionString = _connectionString,
                ContainerName = _containerName,
                FlatBlobListing = false
            };

            var container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.CreateIfNotExistsAsync();

            // Retrieve reference to a blob named "myblob".
            var blockBlob = container.GetBlockBlobReference(_testBlob);
            await blockBlob.UploadFromFileAsync(_testFilePath);
            var blobWithDir = container.GetBlockBlobReference("directory/test-blob2.txt");
            await blobWithDir.UploadFromFileAsync(_testFilePath);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            // delete whole container after running tests
            var container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.DeleteIfExistsAsync();
        }

        [TestMethod]
        public void ListBlobs_ReturnBlockAndDirectory()
        {
            var result = ListTask.ListBlobs(_sourceProperties);

            Assert.AreEqual(2, result.Blobs.Count);
            Assert.AreEqual("Block", result.Blobs[1].BlobType);
            Assert.AreEqual(_testBlob, result.Blobs[1].Name);

            Assert.AreEqual("Directory", result.Blobs[0].BlobType);
        }

        [TestMethod]
        public void ListBlobsWithPrefix()
        {
            _sourceProperties.FlatBlobListing = true;
            var result = ListTask.ListBlobs(_sourceProperties);

            Assert.AreEqual(2, result.Blobs.Count);
        }
    }
}