using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

#pragma warning disable CS1591 

namespace Frends.Community.Azure.Blob
{
    public class DeleteTask
    {
        /// <summary>
        ///     Deletes a single blob from Azure blob storage. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="target">Blob to delete</param>
        /// <param name="connectionProperties"></param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object { bool Success }</returns>
        public static async Task<DeleteOutput> DeleteBlobAsync([PropertyTab]DeleteBlobProperties target,
            [PropertyTab]BlobConnectionProperties connectionProperties, CancellationToken cancellationToken)
        {
            BlobClient blob;

            if (connectionProperties.ConnectionMethod == ConnectionMethod.ConnectionString)
                blob = new BlobClient(connectionProperties.ConnectionString, connectionProperties.ContainerName, target.BlobName);
            else
            {
                var credentials = new ClientSecretCredential(connectionProperties.Connection.TenantID, connectionProperties.Connection.ApplicationID, connectionProperties.Connection.ClientSecret, new ClientSecretCredentialOptions());
                var url = new Uri($"https://{connectionProperties.Connection.StorageAccountName}.blob.core.windows.net/{connectionProperties.ContainerName}/{target.BlobName}");
                blob = new BlobClient(url, credentials);
            }

            if (!await blob.ExistsAsync(cancellationToken)) return new DeleteOutput {Success = true};

            try
            {
                var accessCondition = string.IsNullOrWhiteSpace(target.VerifyETagWhenDeleting)
                    ? new BlobRequestConditions { IfMatch = new global::Azure.ETag(target.VerifyETagWhenDeleting) }
                    : null;

                var result = await blob.DeleteIfExistsAsync(
                    target.SnapshotDeleteOption.ConvertEnum<DeleteSnapshotsOption>(), accessCondition,
                    cancellationToken);

                return new DeleteOutput {Success = result};
            }
            catch (Exception e)
            {
                throw new Exception("DeleteBlobAsync: Error occured while trying to delete blob", e);
            }
        }

        /// <summary>
        ///     Deletes a whole container from Azure blob storage. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="target"></param>
        /// <param name="connectionProperties"></param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object { bool Success }</returns>
        public static async Task<DeleteOutput> DeleteContainerAsync([PropertyTab]DeleteContainerProperties target,
            [PropertyTab]ContainerConnectionProperties connectionProperties, CancellationToken cancellationToken)
        {
            BlobContainerClient container;

            if (connectionProperties.ConnectionMethod == ConnectionMethod.ConnectionString)
                container = Utils.GetBlobContainer(connectionProperties.ConnectionString, target.ContainerName);
            else
                container = Utils.GetBlobContainer(connectionProperties.Connection.ApplicationID, connectionProperties.Connection.TenantID, connectionProperties.Connection.ClientSecret, connectionProperties.Connection.StorageAccountName, target.ContainerName);

            if (!await container.ExistsAsync(cancellationToken)) return new DeleteOutput {Success = true};

            // delete container
            try
            {
                var result = await container.DeleteIfExistsAsync(null, cancellationToken);
                return new DeleteOutput {Success = result};
            }
            catch (Exception e)
            {
                throw new Exception("DeleteContainerAsync: Error occured while trying to delete blob container", e);
            }
        }
    }

    public class DeleteOutput
    {
        public bool Success { get; set; }
    }

    public enum SnapshotDeleteOption
    {
        None,
        IncludeSnapshots,
        DeleteSnapshotsOnly
    }

    public class DeleteContainerProperties
    {
        /// <summary>
        ///     Name of the container to delete
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string ContainerName { get; set; }
    }

    public class ContainerConnectionProperties
    {
        /// <summary>
        ///     Which connection method should be used for connecting to Azure Blob Storage?
        /// </summary>
        [DefaultValue(ConnectionMethod.ConnectionString)]
        public ConnectionMethod ConnectionMethod { get; set; }

        /// <summary>
        ///     Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        [DisplayFormat(DataFormatString = "Text")]
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ConnectionString)]
        public string ConnectionString { get; set; }

        /// <summary>
        ///     OAuth2 connection information.
        /// </summary>
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.OAuth2)]
        public OAuthConnection Connection { get; set; }
    }

    public class BlobConnectionProperties
    {
        /// <summary>
        ///     Which connection method should be used for connecting to Azure Blob Storage?
        /// </summary>
        [DefaultValue(ConnectionMethod.ConnectionString)]
        public ConnectionMethod ConnectionMethod { get; set; }

        /// <summary>
        ///     Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        [DisplayFormat(DataFormatString = "Text")]
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ConnectionString)]
        public string ConnectionString { get; set; }

        /// <summary>
        ///     OAuth2 connection information.
        /// </summary>
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.OAuth2)]
        public OAuthConnection Connection { get; set; }

        /// <summary>
        ///     Name of the container where delete blob exists.
        /// </summary>
        [DisplayName("Blob Container Name")]
        [DefaultValue("test-container")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ContainerName { get; set; }
    }

    public class DeleteBlobProperties
    {
        /// <summary>
        ///     Name of the blob to delete
        /// </summary>
        [DisplayName("Blob name")]
        [DefaultValue("TestFile.xml")]
        [DisplayFormat(DataFormatString = "Text")]
        public string BlobName { get; set; }

        /// <summary>
        ///     Delete blob only if the ETag matches. Leave empty if verification is not needed.
        /// </summary>
        [DisplayName("Verify ETag When Deleting")]
        [DefaultValue("0x9FE13BAA323E5A4")]
        [DisplayFormat(DataFormatString = "Text")]
        public string VerifyETagWhenDeleting { get; set; }

        /// <summary>
        ///     What should be done with blob snapshots?
        /// </summary>
        [DisplayName("Snapshot Delete Option")]
        [DefaultValue(SnapshotDeleteOption.IncludeSnapshots)]
        public SnapshotDeleteOption SnapshotDeleteOption { get; set; }
    }
}