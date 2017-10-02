using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Frends.Community.Azure.Blob
{
    public class Upload
    {
        /// <summary>
        /// Uploads a single file to Azure blob storage.
        /// Will create given container on connection if necessary.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> UploadFileAsync(UploadInput input, UploadOptions options, CancellationToken cancellationToken)
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
            CloudBlobContainer container = Utils.GetBlobContainer(options.ConnectionString, options.ContainerName);

            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // create the container if necessary
            await container.CreateIfNotExistsAsync(cancellationToken);

            // get the destination blob, rename if necessary
            CloudBlob destinationBlob = Utils.GetCloudBlob(container, string.IsNullOrWhiteSpace(options.RenameTo) ? fi.Name : options.RenameTo, options.BlobType);

            // delete blob if user requested overwrite
            if (options.Overwrite)
            {
                await destinationBlob.DeleteIfExistsAsync(cancellationToken);
            }

            // setup the number of the concurrent operations
            TransferManager.Configurations.ParallelOperations = options.ParallelOperations;

            // Use UploadOptions to set ContentType of destination CloudBlob
            Microsoft.WindowsAzure.Storage.DataMovement.UploadOptions uploadOptions = new Microsoft.WindowsAzure.Storage.DataMovement.UploadOptions();

            // Setup the transfer context and track the upload progress
            SingleTransferContext transferContext = new SingleTransferContext
            {
                SetAttributesCallback = (destination) =>
                {
                    CloudBlob cloudBlob = destination as CloudBlob;
                    cloudBlob.Properties.ContentType = MimeMapping.GetMimeMapping(fi.Name);
                }
            };

            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();

            // begin and await for upload to complete
            await TransferManager.UploadAsync(input.SourceFile, destinationBlob, uploadOptions, transferContext, cancellationToken);

            // return uri to uploaded blob
            return destinationBlob.Uri.ToString();
        }
    }

    public class UploadOptions
    {
        /// <summary>
        /// Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayName("Connection String")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Name of the azure blob storage container where the data will be uploaded.
        /// If the container doesn't exist, then it will be created.
        /// Naming: lowercase
        /// Valid chars: alphanumeric and dash, but cannot start or end with dash
        /// </summary>
        [DefaultValue("test-container")]
        [DisplayName("Container Name")]
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
        public string RenameTo { get; set; }

        /// <summary>
        /// Should upload operation overwrite existing file with same name?
        /// </summary>
        [DefaultValue(true)]
        [DisplayName("Overwrite existing file")]
        public bool Overwrite { get; set; }

        [DefaultValue(64)]
        [DisplayName("Parallel Operation")]
        public int ParallelOperations { get; set; }
    }

    public class UploadInput
    {
        [DefaultValue(@"c:\temp\TestFile.xml")]
        [DisplayName("Source File")]
        public string SourceFile { get; set; }
    }

    public enum AzureBlobType
    {
        Append,
        Block,
        Page
    }
}
