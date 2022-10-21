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
        ///     Connection string for Azure Storage Account.
        /// </summary>
        private readonly string _connectionString = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ConnString");
        private readonly string _appID = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_AppID");
        private readonly string _tenantID = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_TenantID");
        private readonly string _clientSecret = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ClientSecret");
        private readonly string _storageAccount = "testsorage01";

        [TestMethod]
        public async Task DeleteBlobAsync_ShouldReturnTrueWithNonexistingBlob()
        {
            var containerName = "test" + Guid.NewGuid().ToString();

            var input = new DeleteBlobProperties
            {
                BlobName = Guid.NewGuid().ToString()
            };
            var connection = new BlobConnectionProperties
            {
                ConnectionMethod = ConnectionMethod.ConnectionString,
                ConnectionString = _connectionString,
                ContainerName = containerName
            };
            var container = Utils.GetBlobContainer(_connectionString, containerName);
            await container.CreateIfNotExistsAsync();

            var result = await DeleteTask.DeleteBlobAsync(input, connection, new CancellationToken());

            Assert.IsTrue(result.Success, "DeleteBlob should've returned true when trying to delete non existing blob");
            await DeleteContainer(containerName);
        }

        [TestMethod]
        public async Task DeleteBlobAsync_ShouldReturnTrueWithNonexistingContainer()
        {
            var containerName = "test" + Guid.NewGuid().ToString();

            var input = new DeleteBlobProperties
            {
                BlobName = Guid.NewGuid().ToString()
            };
            var options = new BlobConnectionProperties
            {
                ConnectionMethod = ConnectionMethod.ConnectionString,
                ConnectionString = _connectionString,
                ContainerName = containerName
            };

            var result = await DeleteTask.DeleteBlobAsync(input, options, new CancellationToken());

            Assert.IsTrue(result.Success,
                "DeleteBlob should've returned true when trying to delete blob in non existing container");
            await DeleteContainer(containerName);
        }

        [TestMethod]
        public async Task DeleteBlobAsync_AccessTokenAuthenticationTest()
        {
            var containerName = "test" + Guid.NewGuid().ToString();
            var input = new DeleteBlobProperties
            {
                BlobName = Guid.NewGuid().ToString()
            };
            var oauth = new OAuthConnection
            {
                StorageAccountName = _storageAccount,
                ApplicationID = _appID,
                TenantID = _tenantID,
                ClientSecret = _clientSecret
            };
            var connection = new BlobConnectionProperties
            {
                ConnectionMethod = ConnectionMethod.OAuth2,
                ContainerName = containerName,
                Connection = oauth
            };
            var container = Utils.GetBlobContainer(_connectionString, containerName);
            await container.CreateIfNotExistsAsync();

            var result = await DeleteTask.DeleteBlobAsync(input, connection, new CancellationToken());

            Assert.IsTrue(result.Success, "DeleteBlob should've returned true when trying to delete non existing blob");
            await DeleteContainer(containerName);
        }

        [TestMethod]
        public async Task DeleteContainerAsync_ShouldReturnTrueWithNonexistingContainer()
        {
            var inputProperties = new DeleteContainerProperties {ContainerName = Guid.NewGuid().ToString()};
            var connection = new ContainerConnectionProperties {ConnectionMethod = ConnectionMethod.ConnectionString, ConnectionString = _connectionString};

            var result = await DeleteTask.DeleteContainerAsync(inputProperties, connection, new CancellationToken());

            Assert.IsTrue(result.Success,
                "DeleteContainer should've returned true when trying to delete non existing container");
        }

        [TestMethod]
        public async Task DeleteContainerAsync_AccessTokenAuthenticationTest()
        {
            var inputProperties = new DeleteContainerProperties { ContainerName = Guid.NewGuid().ToString() };
            var oauth = new OAuthConnection
            {
                StorageAccountName = _storageAccount,
                ApplicationID = _appID,
                TenantID = _tenantID,
                ClientSecret = _clientSecret
            };
            var connection = new ContainerConnectionProperties
            { 
                ConnectionMethod = ConnectionMethod.OAuth2,
                Connection = oauth
            };

            var result = await DeleteTask.DeleteContainerAsync(inputProperties, connection, new CancellationToken());

            Assert.IsTrue(result.Success,
                "DeleteContainer should've returned true when trying to delete non existing container");
        }

        private async Task DeleteContainer(string containerName)
        {
            var container = Utils.GetBlobContainer(_connectionString, containerName);
            await container.DeleteIfExistsAsync();
        }
    }
}