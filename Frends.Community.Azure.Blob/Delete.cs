using Frends.Tasks.Attributes;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591 

namespace Frends.Community.Azure.Blob
{
    public class Delete
    {
        /// <summary>
        /// Deletes a single blob from Azure blob storage. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="target">Blob to delete</param>
        /// <param name="connectionProperties"></param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object { bool Success }</returns>
        public static async Task<DeleteOutput> DeleteBlobAsync(DeleteBlobProperties target, BlobConnectionProperties connectionProperties, CancellationToken cancellationToken)
        {
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // get container
            CloudBlobContainer container = Utils.GetBlobContainer(connectionProperties.ConnectionString, connectionProperties.ContainerName);

            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // if container doesn't exist, exit ok
            if (!await container.ExistsAsync())
            {
                return new DeleteOutput { Success = true };
            }

            // get the destination blob, rename if necessary
            CloudBlob blob = Utils.GetCloudBlob(container, target.BlobName, target.BlobType);
            
            if(!await blob.ExistsAsync(cancellationToken))
            {
                return new DeleteOutput { Success = true };
            }

            try
            {
                var result = await blob.DeleteIfExistsAsync(cancellationToken);
                return new DeleteOutput { Success = result };
            }
            catch(Exception e)
            {
                throw new Exception("DeleteBlobAsync: Error occured while trying to delete blob", e);
            }
        }

        /// <summary>
        /// Deletes a whole container from Azure blob storage. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="target"></param>
        /// <param name="ConnectionProperties"></param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object { bool Success }</returns>
        public static async Task<DeleteOutput> DeleteContainerAsync(DeleteContainerProperties target, ContainerConnectionProperties ConnectionProperties, CancellationToken cancellationToken)
        {
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // get container
            CloudBlobContainer container = Utils.GetBlobContainer(ConnectionProperties.ConnectionString, target.ContainerName);

            if(!await container.ExistsAsync())
            {
                return new DeleteOutput { Success = true };
            }

            // delete container
            try
            { 
                var result = await container.DeleteIfExistsAsync(cancellationToken);
                return new DeleteOutput { Success = result };
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

    public class DeleteContainerProperties
    {
        /// <summary>
        /// Name of the container to delete
        /// </summary>
        [DefaultDisplayType(DisplayType.Text)]
        public string ContainerName { get; set; }
    }

    public class ContainerConnectionProperties
    {
        /// <summary>
        /// Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        [DefaultDisplayType(DisplayType.Text)]
        public string ConnectionString { get; set; }
    }

    public class BlobConnectionProperties
    {
        /// <summary>
        /// Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        [DefaultDisplayType(DisplayType.Text)]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Name of the container where delete blob exists.
        /// </summary>
        [DisplayName("Blob Container Name")]
        [DefaultValue("test-container")]
        [DefaultDisplayType(DisplayType.Text)]
        public string ContainerName { get; set; }

    }

    public class DeleteBlobProperties
    {
        /// <summary>
        /// Name of the blob to delete
        /// </summary>
        [DisplayName("Blob name")]
        [DefaultValue("TestFile.xml")]
        [DefaultDisplayType(DisplayType.Text)]
        public string BlobName { get; set; }

        /// <summary>
        /// Type of blob to delete: Append, Block or Page
        /// </summary>
        [DisplayName("Blob Type")]
        [DefaultValue(AzureBlobType.Block)]
        public AzureBlobType BlobType { get; set; }
    }
}
