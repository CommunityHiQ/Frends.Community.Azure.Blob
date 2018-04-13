using Frends.Tasks.Attributes;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

#pragma warning disable CS1591 

namespace Frends.Community.Azure.Blob
{
    public class UploadTask
    {
        /// <summary>
        /// Uploads a single file to Azure blob storage. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// Will create given container on connection if necessary.
        /// </summary>
        /// <returns>Object { string Uri, string SourceFile }</returns>
        public static async Task<UploadOutput> UploadFileAsync(UploadInput input, DestinationProperties destinationProperties, CancellationToken cancellationToken)
        {
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // check that source file exists
            FileInfo fi = new FileInfo(input.SourceFile);
            if (!fi.Exists)
            {
                throw new ArgumentException($"Source file {input.SourceFile} does not exist", nameof(input.SourceFile));
            }

            // get container
            CloudBlobContainer container = Utils.GetBlobContainer(destinationProperties.ConnectionString, destinationProperties.ContainerName);

            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // create the container if necessary
            await container.CreateIfNotExistsAsync(cancellationToken);

            // get the destination blob, rename if necessary
            CloudBlob destinationBlob = Utils.GetCloudBlob(container, string.IsNullOrWhiteSpace(destinationProperties.RenameTo) ? fi.Name : destinationProperties.RenameTo, destinationProperties.BlobType);

            // delete blob if user requested overwrite
            if (destinationProperties.Overwrite)
            {
                await destinationBlob.DeleteIfExistsAsync(cancellationToken);
            }

            // setup the number of the concurrent operations
            TransferManager.Configurations.ParallelOperations = destinationProperties.ParallelOperations;

            // Use UploadOptions to set ContentType of destination CloudBlob
            Microsoft.WindowsAzure.Storage.DataMovement.UploadOptions uploadOptions = new UploadOptions();

            // Setup the transfer context and track the upload progress
            SingleTransferContext transferContext = new SingleTransferContext
            {
                SetAttributesCallback = (destination) =>
                {
                    CloudBlob cloudBlob = destination as CloudBlob;
                    cloudBlob.Properties.ContentType = MimeMapping.GetMimeMapping(fi.Name);
                }
            };

            // begin and await for upload to complete
            try
            {
                await TransferManager.UploadAsync(input.SourceFile, destinationBlob, uploadOptions, transferContext, cancellationToken);
            }
            catch (Exception e)
            {
                throw new Exception("UploadFileAsync: Error occured while uploading file to blob storage", e);
            }

            // return uri to uploaded blob and source file path
            return new UploadOutput { SourceFile = input.SourceFile, Uri = destinationBlob.Uri.ToString() };
        }
    }

    public class UploadOutput
    {
        public string SourceFile { get; set; }
        public string Uri { get; set; }
    }

    public class DestinationProperties
    {
        /// <summary>
        /// Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        [DefaultDisplayType(DisplayType.Text)]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Name of the azure blob storage container where the data will be uploaded.
        /// If the container doesn't exist, then it will be created.
        /// Naming: lowercase
        /// Valid chars: alphanumeric and dash, but cannot start or end with dash
        /// </summary>
        [DefaultValue("test-container")]
        [DisplayName("Container Name")]
        [DefaultDisplayType(DisplayType.Text)]
        public string ContainerName { get; set; }

        /// <summary>
        /// Azure blob type to upload: Append, Block or Page
        /// </summary>
        [DefaultValue(AzureBlobType.Block)]
        [DisplayName("Blob Type")]
        public AzureBlobType BlobType { get; set; }

        /// <summary>
        /// Source file can be renamed to this name in azure blob storage
        /// </summary>
        [DefaultValue("")]
        [DisplayName("Rename source file")]
        [DefaultDisplayType(DisplayType.Text)]
        public string RenameTo { get; set; }

        /// <summary>
        /// Should upload operation overwrite existing file with same name?
        /// </summary>
        [DefaultValue(true)]
        [DisplayName("Overwrite existing file")]
        public bool Overwrite { get; set; }


        /// <summary>
        /// How many work items to process concurrently.
        /// </summary>
        [DefaultValue(64)]
        [DisplayName("Parallel Operation")]
        public int ParallelOperations { get; set; }
    }

    public class UploadInput
    {
        [DefaultValue(@"c:\temp\TestFile.xml")]
        [DisplayName("Source File")]
        [DefaultDisplayType(DisplayType.Text)]
        public string SourceFile { get; set; }
    }

    public enum AzureBlobType
    {
        Append,
        Block,
        Page
    }
}
