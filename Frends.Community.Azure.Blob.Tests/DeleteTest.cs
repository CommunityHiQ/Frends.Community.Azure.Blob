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
    public class DeleteTest
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
        public async Task DeleteBlobAsync_ShouldReturnTrueWithNonexistingBlob()
        {
            var input = new DeleteTarget
            {
                BlobName = Guid.NewGuid().ToString(),
                ContainerName = "test-container"
            };
            var options = new DeleteOptions
            {
                ConnectionString = _connectionString
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.CreateIfNotExistsAsync();

            bool result = await Delete.DeleteBlobAsync(input, options, new CancellationToken());

            Assert.IsTrue(result, "DeleteBlob should've returned true when trying to delete non existing blob");
        }

        [TestMethod]
        public async Task DeleteBlobAsync_ShouldReturnTrueWithNonexistingContainer()
        {
            var input = new DeleteTarget
            {
                BlobName = Guid.NewGuid().ToString(),
                ContainerName = Guid.NewGuid().ToString()
            };
            var options = new DeleteOptions
            {
                ConnectionString = _connectionString
            };

            var result = await Delete.DeleteBlobAsync(input, options, new CancellationToken());

            Assert.IsTrue(result, "DeleteBlob should've returned true when trying to delete blob in non existing container");
        }
        
        [TestMethod]
        public async Task DeleteContainerAsync_ShouldReturnTrueWithNonexistingContainer()
        {
            var container = Guid.NewGuid().ToString();
            var options = new DeleteOptions { ConnectionString = _connectionString };

            var result = await Delete.DeleteContainerAsync(container, options, new CancellationToken());

            Assert.IsTrue(result, "DeleteContainer should've returned true when trying to delete non existing container");
        }
    }
}
