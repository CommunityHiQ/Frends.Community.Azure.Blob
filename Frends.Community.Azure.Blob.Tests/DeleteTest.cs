using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class DeleteTest
    {
        /// <summary>
        ///     Connection string for Azure Storage Emulator
        /// </summary>
        private readonly string _connectionString = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ConnString");

        /// <summary>
        ///     Container name for tests
        /// </summary>
        private readonly string _containerName = "test-container";

        [TestCleanup]
        public async Task Cleanup()
        {
            // delete whole container after running tests
            var container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.DeleteIfExistsAsync();
        }

        [TestMethod]
        public async Task DeleteBlobAsync_ShouldReturnTrueWithNonexistingBlob()
        {
            var input = new DeleteBlobProperties
            {
                BlobName = Guid.NewGuid().ToString()
            };
            var connection = new BlobConnectionProperties
            {
                ConnectionString = _connectionString,
                ContainerName = _containerName
            };
            var container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.CreateIfNotExistsAsync();

            var result = await DeleteTask.DeleteBlobAsync(input, connection, new CancellationToken());

            Assert.IsTrue(result.Success, "DeleteBlob should've returned true when trying to delete non existing blob");
        }

        [TestMethod]
        public async Task DeleteBlobAsync_ShouldReturnTrueWithNonexistingContainer()
        {
            var input = new DeleteBlobProperties
            {
                BlobName = Guid.NewGuid().ToString()
            };
            var options = new BlobConnectionProperties
            {
                ConnectionString = _connectionString,
                ContainerName = Guid.NewGuid().ToString()
            };

            var result = await DeleteTask.DeleteBlobAsync(input, options, new CancellationToken());

            Assert.IsTrue(result.Success,
                "DeleteBlob should've returned true when trying to delete blob in non existing container");
        }

        [TestMethod]
        public async Task DeleteContainerAsync_ShouldReturnTrueWithNonexistingContainer()
        {
            var inputProperties = new DeleteContainerProperties {ContainerName = Guid.NewGuid().ToString()};
            var connection = new ContainerConnectionProperties {ConnectionString = _connectionString};

            var result = await DeleteTask.DeleteContainerAsync(inputProperties, connection, new CancellationToken());

            Assert.IsTrue(result.Success,
                "DeleteContainer should've returned true when trying to delete non existing container");
        }
    }
}