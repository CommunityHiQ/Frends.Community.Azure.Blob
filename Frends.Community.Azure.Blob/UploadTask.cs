using System.ComponentModel.DataAnnotations;
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

            try
            {
                if (destinationProperties.CreateContainerIfItDoesNotExist)
                {
                    // create the container if necessary
                    await container.CreateIfNotExistsAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Checking if container exists or creating new container caused an exception.", ex);
            }

            // get the destination blob, rename if necessary
            CloudBlob destinationBlob = Utils.GetCloudBlob(container, 
                string.IsNullOrWhiteSpace(destinationProperties.RenameTo) ? fi.Name : destinationProperties.RenameTo, 
                destinationProperties.BlobType);

            var contentType = string.IsNullOrWhiteSpace(destinationProperties.ContentType) ? 
                    MimeMapping.GetMimeMapping(fi.Name) :
                    destinationProperties.ContentType;
            
            var encoding = destinationProperties.FileEncoding.ConvertToEncoding();

            // delete blob if user requested overwrite
            if (destinationProperties.Overwrite)
            {
                await destinationBlob.DeleteIfExistsAsync(cancellationToken);
            }

            // setup the number of the concurrent operations
            TransferManager.Configurations.ParallelOperations = destinationProperties.ParallelOperations;

            // Use UploadOptions to set ContentType of destination CloudBlob
            UploadOptions uploadOptions = new UploadOptions();

            var progressHandler = new Progress<TransferStatus>(progress =>
            {
                Console.WriteLine("Bytes uploaded: {0}", progress.BytesTransferred);
            });

            // Setup the transfer context and track the upload progress
            SingleTransferContext transferContext = new SingleTransferContext
            {
                SetAttributesCallback = (destination) =>
                {
                    CloudBlob cloudBlob = destination as CloudBlob;
                    if (cloudBlob == null) throw new ArgumentNullException();
                    cloudBlob.Properties.ContentType = contentType;
                    cloudBlob.Properties.ContentEncoding = input.Compress ? "gzip" : encoding.WebName;
                },

                ProgressHandler = progressHandler
            };
            
            // begin and await for upload to complete
            try
            {
                using (var stream = Utils.GetStream(input.Compress, input.ContentsOnly, encoding, fi))
                {
                    await TransferManager.UploadAsync(
                        stream,
                        destinationBlob, uploadOptions, transferContext,
                        cancellationToken);
                }
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
        [DisplayFormat(DataFormatString = "Text")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Name of the azure blob storage container where the data will be uploaded.
        /// Naming: lowercase
        /// Valid chars: alphanumeric and dash, but cannot start or end with dash
        /// </summary>
        [DefaultValue("test-container")]
        [DisplayName("Container Name")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ContainerName { get; set; }

        /// <summary>
        /// Determines if the container should be created if it does not exist
        /// </summary>
        [DisplayName("Create container if it does not exist")]
        public bool CreateContainerIfItDoesNotExist { get; set; }

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
        [DisplayFormat(DataFormatString = "Text")]
        public string RenameTo { get; set; }

        /// <summary>
        /// Set desired content-type. If empty, task tries to guess from mime-type.
        /// </summary>
        [DefaultValue("")]
        [DisplayName("Force Content-Type")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ContentType { get; set; }

        /// <summary>
        /// Set desired content-encoding. Defaults to UTF8 BOM.
        /// </summary>
        [DefaultValue("")]
        [DisplayName("Force Content-Encoding")]
        [DisplayFormat(DataFormatString = "Text")]
        public string FileEncoding { get; set; }

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
        [DisplayFormat(DataFormatString = "Text")]
        public string SourceFile { get; set; }

        /// <summary>
        /// Uses stream to read file content.
        /// </summary>
        [DefaultValue(false)]
        [DisplayName("Stream content only")]
        public bool ContentsOnly { get; set; }

        /// <summary>
        /// Works only when transferring stream content.
        /// </summary>
        [DefaultValue(false)]
        [DisplayName("Gzip compression")]
        public bool Compress { get; set; }
    }

    public enum AzureBlobType
    {
        Append,
        Block,
        Page
    }
}
