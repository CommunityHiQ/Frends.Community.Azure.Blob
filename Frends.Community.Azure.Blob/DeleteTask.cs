using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
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
        public static async Task<DeleteOutput> DeleteBlobAsync(DeleteBlobProperties target,
            BlobConnectionProperties connectionProperties, CancellationToken cancellationToken)
        {

            // get Blob Client
            var blob = new BlobClient(connectionProperties.ConnectionString, connectionProperties.ContainerName, target.BlobName);

            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

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
        public static async Task<DeleteOutput> DeleteContainerAsync(DeleteContainerProperties target,
            ContainerConnectionProperties connectionProperties, CancellationToken cancellationToken)
        {
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // get container
            var container = Utils.GetBlobContainer(connectionProperties.ConnectionString, target.ContainerName);

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
        ///     Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ConnectionString { get; set; }
    }

    public class BlobConnectionProperties
    {
        /// <summary>
        ///     Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ConnectionString { get; set; }

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