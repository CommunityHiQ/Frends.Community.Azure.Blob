using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestFixture]
    class ListTests
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
        /// Test blob name
        /// </summary>
        private readonly string _testBlob = "test-blob.txt";

        /// <summary>
        /// Some random file for test purposes
        /// </summary>
        private string _testFilePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\TestFiles\TestFile.xml";

        private ListSourceProperties _sourceProperties;

        [SetUp]
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

        [TearDown]
        public async Task Cleanup()
        {
            // delete whole container after running tests
            CloudBlobContainer container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.DeleteIfExistsAsync();
        }

        [Test]
        public void ListBlobs_ReturnBlockAndDirectory()
        {
            var result = ListTask.ListBlobs(_sourceProperties);

            Assert.AreEqual(2, result.Blobs.Count);
            Assert.AreEqual("Block", result.Blobs[1].BlobType);
            Assert.AreEqual(_testBlob, result.Blobs[1].Name);

            Assert.AreEqual("Directory", result.Blobs[0].BlobType);
        }

        [Test]
        public void ListBlobsWithPrefix()
        {
            _sourceProperties.FlatBlobListing = true;
            var result = ListTask.ListBlobs(_sourceProperties);

            Assert.AreEqual(2, result.Blobs.Count);
        }
    }
}
