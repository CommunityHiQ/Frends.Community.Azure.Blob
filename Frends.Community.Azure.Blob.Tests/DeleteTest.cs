using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestFixture]
    class DeleteTest
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

        [TearDown]
        public async Task Cleanup()
        {
            // delete whole container after running tests
            CloudBlobContainer container = Utils.GetBlobContainer(_connectionString, _containerName);
            await container.DeleteIfExistsAsync();
        }

        [Test]
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

            var result = await Delete.DeleteBlobAsync(input, connection, new CancellationToken());

            Assert.IsTrue(result.Success, "DeleteBlob should've returned true when trying to delete non existing blob");
        }

        [Test]
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

            var result = await Delete.DeleteBlobAsync(input, options, new CancellationToken());

            Assert.IsTrue(result.Success, "DeleteBlob should've returned true when trying to delete blob in non existing container");
        }

        [Test]
        public async Task DeleteContainerAsync_ShouldReturnTrueWithNonexistingContainer()
        {
            var inputProperties = new DeleteContainerProperties { ContainerName = Guid.NewGuid().ToString() };
            var connection = new ContainerConnectionProperties { ConnectionString = _connectionString };

            var result = await Delete.DeleteContainerAsync(inputProperties, connection, new CancellationToken());

            Assert.IsTrue(result.Success, "DeleteContainer should've returned true when trying to delete non existing container");
        }
    }
}
