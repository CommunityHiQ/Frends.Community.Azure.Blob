using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using MimeMapping;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class UploadTask
    {
        /// <summary>
        ///     Uploads a single file to Azure blob storage. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        ///     Will create given container on connection if necessary.
        /// </summary>
        /// <returns>Object { string Uri, string SourceFile }</returns>
        public static async Task<UploadOutput> UploadFileAsync([PropertyTab]UploadInput input,
            [PropertyTab]DestinationProperties destinationProperties, CancellationToken cancellationToken)
        {
            // check that source file exists
            var fi = new FileInfo(input.SourceFile);
            if (!fi.Exists)
                throw new ArgumentException($"Source file {input.SourceFile} does not exist", nameof(input.SourceFile));

            BlobContainerClient container;

            if (destinationProperties.ConnectionMethod == ConnectionMethod.ConnectionString)
                container = Utils.GetBlobContainer(destinationProperties.ConnectionString, destinationProperties.ContainerName);
            else
                container = Utils.GetBlobContainer(destinationProperties.Connection.ApplicationID, destinationProperties.Connection.TenantID, destinationProperties.Connection.ClientSecret, destinationProperties.Connection.StorageAccountName, destinationProperties.ContainerName);

            try
            {
                if (destinationProperties.CreateContainerIfItDoesNotExist)
                    await container.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Checking if container exists or creating new container caused an exception.", ex);
            }

            string fileName;
            if (string.IsNullOrWhiteSpace(destinationProperties.RenameTo) && input.Compress)
                fileName = fi.Name + ".gz";
            else if (string.IsNullOrWhiteSpace(destinationProperties.RenameTo))
                fileName = fi.Name;
            else
                fileName = destinationProperties.RenameTo;

            Dictionary<string, string> tags = input.Tags?.ToDictionary(tag => tag.Name, tag => tag.Value);

            // return uri to uploaded blob and source file path

            switch (destinationProperties.BlobType)
            {
                case AzureBlobType.Append:
                    return await AppendBlob(input, destinationProperties, fi, fileName, tags, cancellationToken);
                case AzureBlobType.Page:
                    return await UploadPageBlob(input, destinationProperties, fi, fileName, cancellationToken);
                default:
                    return await UploadBlockBlob(input, destinationProperties, fi, fileName, tags, cancellationToken);
            }
        }

        private static Encoding GetEncoding(string target)
        {
            switch (target.ToLower())
            {
                case "utf-8":
                    return Encoding.UTF8;
                case "utf-7":
                    return Encoding.UTF7;
                case "utf-32":
                    return Encoding.UTF32;
                case "unicode":
                    return Encoding.Unicode;
                case "ascii":
                    return Encoding.ASCII;
                default:
                    return Encoding.UTF8;
            }
        }

        private static async Task<UploadOutput> UploadBlockBlob(UploadInput input,
            DestinationProperties destinationProperties,
            FileInfo fi,
            string fileName,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken)
        {
            var blob = Utils.GetBlobClient(destinationProperties.ConnectionMethod, destinationProperties.ConnectionString, destinationProperties.Connection, destinationProperties.ContainerName, fileName);

            var contentType = string.IsNullOrWhiteSpace(destinationProperties.ContentType)
                ? MimeUtility.GetMimeMapping(fi.Name)
                : destinationProperties.ContentType;

            var encoding = GetEncoding(destinationProperties.FileEncoding);

            // delete blob if user requested overwrite
            if (destinationProperties.Overwrite) await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, cancellationToken);

            var progressHandler = new Progress<long>(progress =>
            {
                Console.WriteLine("Bytes uploaded: {0}", progress);
            });

            // setup the number of the concurrent operations
            var uploadOptions = new BlobUploadOptions
            {
                ProgressHandler = progressHandler,
                TransferOptions = new StorageTransferOptions { MaximumConcurrency = destinationProperties.ParallelOperations },
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType, ContentEncoding = input.Compress ? "gzip" : encoding.WebName },
                Tags = tags
            };

            // begin and await for upload to complete
            try
            {
                using (var stream = Utils.GetStream(input.Compress, input.ContentsOnly, encoding, fi))
                {
                    await blob.UploadAsync(stream, uploadOptions, cancellationToken);
                }
            }
            catch (Exception e)
            {
                throw new Exception("UploadFileAsync: Error occured while uploading file to blob storage", e);
            }

            return new UploadOutput { SourceFile = input.SourceFile, Uri = blob.Uri.ToString() };
        }

        private static async Task<UploadOutput> AppendBlob(UploadInput input,
            DestinationProperties destinationProperties,
            FileInfo fi,
            string fileName,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken)
        {
            var blob = Utils.GetAppendBlobClient(destinationProperties.ConnectionMethod, destinationProperties.ConnectionString, destinationProperties.Connection, destinationProperties.ContainerName, fileName);

            var encoding = GetEncoding(destinationProperties.FileEncoding);

            // delete blob if user requested overwrite
            if (destinationProperties.Overwrite)
            {
                await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, cancellationToken);

                var contentType = string.IsNullOrWhiteSpace(destinationProperties.ContentType)
                ? MimeUtility.GetMimeMapping(fi.Name)
                : destinationProperties.ContentType;

                var uploadOptions = new AppendBlobCreateOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType, ContentEncoding = input.Compress ? "gzip" : encoding.WebName },
                    Tags = tags
                };

                await blob.CreateAsync(uploadOptions, cancellationToken);
            }

            var progressHandler = new Progress<long>(progress =>
            {
                Console.WriteLine("Bytes uploaded: {0}", progress);
            });

            // begin and await for upload to complete
            try
            {
                using (var stream = Utils.GetStream(false, true, encoding, fi))
                {
                    await blob.AppendBlockAsync(stream, null, null, progressHandler, cancellationToken);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error occured while appending a block.", e);
            }

            return new UploadOutput { SourceFile = input.SourceFile, Uri = blob.Uri.ToString() };
        }

        private static async Task<UploadOutput> UploadPageBlob(UploadInput input,
            DestinationProperties destinationProperties,
            FileInfo fi,
            string fileName,
            CancellationToken cancellationToken)
        {
            var blob = Utils.GetPageBlobClient(destinationProperties.ConnectionMethod, destinationProperties.ConnectionString, destinationProperties.Connection, destinationProperties.ContainerName, fileName); ;

            var encoding = GetEncoding(destinationProperties.FileEncoding);

            // delete blob if user requested overwrite
            if (destinationProperties.Overwrite) await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, cancellationToken);

            var progressHandler = new Progress<long>(progress =>
            {
                Console.WriteLine("Bytes uploaded: {0}", progress);
            });

            // begin and await for upload to complete
            try
            {
                using (var stream = Utils.GetStream(false, true, encoding, fi))
                {
                    await blob.UploadPagesAsync(stream, 512L, null, null, progressHandler, cancellationToken);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error occured while uploading page blob", e);
            }

            return new UploadOutput { SourceFile = input.SourceFile, Uri = blob.Uri.ToString() };
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
        ///     Which connection method should be used for connecting to Azure Blob Storage?
        /// </summary>
        [DefaultValue(ConnectionMethod.ConnectionString)]
        public ConnectionMethod ConnectionMethod { get; set; }

        /// <summary>
        ///     Connection string to Azure storage
        /// </summary>
        [DisplayName("Connection String")]
        [DisplayFormat(DataFormatString = "Text")]
        [PasswordPropertyText]
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ConnectionString)]
        public string ConnectionString { get; set; }

        /// <summary>
        ///     OAuth2 connection information.
        /// </summary>
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.OAuth2)]
        public OAuthConnection Connection { get; set; }

        /// <summary>
        ///     Name of the azure blob storage container where the data will be uploaded.
        ///     Naming: lowercase
        ///     Valid chars: alphanumeric and dash, but cannot start or end with dash
        /// </summary>
        [DefaultValue("test-container")]
        [DisplayName("Container Name")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ContainerName { get; set; }

        /// <summary>
        ///     Determines if the container should be created if it does not exist
        /// </summary>
        [DisplayName("Create container if it does not exist")]
        public bool CreateContainerIfItDoesNotExist { get; set; }

        /// <summary>
        ///     Azure blob type to upload: Append, Block or Page
        /// </summary>
        [DefaultValue(AzureBlobType.Block)]
        [DisplayName("Blob Type")]
        public AzureBlobType BlobType { get; set; }

        /// <summary>
        ///     Source file can be renamed to this name in azure blob storage
        /// </summary>
        [DefaultValue("")]
        [DisplayName("Rename source file")]
        [DisplayFormat(DataFormatString = "Text")]
        public string RenameTo { get; set; }

        /// <summary>
        ///     Set desired content-type. If empty, task tries to guess from mime-type.
        /// </summary>
        [DefaultValue("")]
        [DisplayName("Force Content-Type")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ContentType { get; set; }

        /// <summary>
        ///     Set desired content-encoding. Defaults to UTF8 BOM.
        /// </summary>
        [DefaultValue("")]
        [DisplayName("Force Content-Encoding")]
        [DisplayFormat(DataFormatString = "Text")]
        public string FileEncoding { get; set; }

        /// <summary>
        ///     Should upload operation overwrite existing file with same name?
        /// </summary>
        [DefaultValue(true)]
        [DisplayName("Overwrite existing file")]
        public bool Overwrite { get; set; }

        /// <summary>
        ///     How many work items to process concurrently.
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
        ///     Uses stream to read file content.
        /// </summary>
        [DefaultValue(false)]
        [DisplayName("Stream content only")]
        public bool ContentsOnly { get; set; }

        /// <summary>
        ///     Works only when transferring stream content.
        /// </summary>
        [DefaultValue(false)]
        [DisplayName("Gzip compression")]
        public bool Compress { get; set; }

        /// <summary>
        ///     Tags for the uploaded blob. Should be set to null if the storage account does not support tags.
        /// </summary>
        public Tag[] Tags { get; set; }
    }

    public class Tag
    {
        /// <summary>
        ///     Name of the tag.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string Name { get; set; }

        /// <summary>
        ///     Value of the tag.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string Value { get; set; }
    }

    public enum AzureBlobType
    {
        Append,
        Block,
        Page
    }
}