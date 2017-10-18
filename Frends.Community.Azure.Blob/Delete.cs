using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.Community.Azure.Blob
{
    public class Delete
    {
        /// <summary>
        /// Deletes a single blob from Azure blob storage.
        /// </summary>
        /// <param name="target">Blob to delete</param>
        /// <param name="options">Connection options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Boolean indicating whether the operation was successful or not</returns>
        public static async Task<bool> DeleteBlobAsync(DeleteTarget target, DeleteOptions options, CancellationToken cancellationToken)
        {
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // get container
            CloudBlobContainer container = Utils.GetBlobContainer(options.ConnectionString, target.ContainerName);

            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // if container doesn't exist, exit ok
            if (!await container.ExistsAsync())
            {
                return true;
            }

            // get the destination blob, rename if necessary
            CloudBlob blob = Utils.GetCloudBlob(container, target.BlobName, target.BlobType);
            
            if(!await blob.ExistsAsync(cancellationToken))
            {
                return true;
            }

            try
            {
                return await blob.DeleteIfExistsAsync(cancellationToken);
            }
            catch(Exception e)
            {
                throw new Exception("DeleteBlobAsync: Error occured while trying to delete blob", e);
            }
        }

        /// <summary>
        /// Deletes a whole container from Azure blob storage
        /// </summary>
        /// <param name="containerName">Name of the container to delete</param>
        /// <param name="options">Connection options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Boolean indicating whether the operation was successful or not</returns>
        public static async Task<bool> DeleteContainerAsync(string containerName, DeleteOptions options, CancellationToken cancellationToken)
        {
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // get container
            CloudBlobContainer container = Utils.GetBlobContainer(options.ConnectionString, containerName);

            if(!await container.ExistsAsync())
            {
                return true;
            }

            // delete container
            try
            { 
                return await container.DeleteIfExistsAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new Exception("DeleteContainerAsync: Error occured while trying to delete blob container", e);
            }
        }
    }

    public class DeleteOptions
    {
        /// <summary>
        /// Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        public string ConnectionString { get; set; }
    }

    public class DeleteTarget
    {
        /// <summary>
        /// Name of the blob to delete
        /// </summary>
        [DisplayName("Blob name")]
        [DefaultValue("TestFile.xml")]
        public string BlobName { get; set; }

        /// <summary>
        /// Name of the container where delete blob exists.
        /// </summary>
        [DisplayName("Blob Container Name")]
        [DefaultValue("test-container")]
        public string ContainerName { get; set; }

        /// <summary>
        /// Type of blob to delete: Append, Block or Page
        /// </summary>
        [DisplayName("Blob Type")]
        [DefaultValue(AzureBlobType.Block)]
        public AzureBlobType BlobType { get; set; }
    }
}
