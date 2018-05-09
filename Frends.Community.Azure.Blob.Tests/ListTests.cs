using TestConfigurationHandler;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class ListTests
    {
        /// <summary>
        /// Container name for tests
        /// </summary>
        private readonly string _containerName = "test-container";

        /// <summary>
        /// Connection string for Azure Storage Emulator
        /// </summary>
        private readonly string _connectionString = ConfigHandler.ReadConfigValue("HiQ.AzureBlobStorage.ConnString");

        /// <summary>
        /// Test blob name
        /// </summary>
        private readonly string _testBlob = "test-blob.txt";

        /// <summary>
        /// Some random file for test purposes
        /// </summary>
        private string _testFilePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\TestFiles\TestFile.xml";

        private ListSourceProperties _sourceProperties;

        [TestInitialize]
        public async Task TestSetup()
        {
            _sourceProperties = new ListSourceProperties { ConnectionString = _connectionString, ContainerName = _containerName, FlatBlobListing = false };

            var container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.CreateIfNotExistsAsync();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(_testBlob);
            await blockBlob.UploadFromFileAsync(_testFilePath);
            var blobWithDir = container.GetBlockBlobReference("directory/test-blob2.txt");
            await blobWithDir.UploadFromFileAsync(_testFilePath);


        }

        [TestCleanup]
        public async Task Cleanup()
        {
            // delete whole container after running tests
            CloudBlobContainer container = Utils.GetBlobContainer(_connectionString, _containerName);
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
