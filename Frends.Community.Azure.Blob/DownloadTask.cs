using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class DownloadTask
    {
        /// <summary>
        ///     Downloads Blob to a file. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { string FileName, string Directory, string FullPath}</returns>
        public static async Task<DownloadBlobOutput> DownloadBlobAsync(SourceProperties source,
            DestinationFileProperties destination, CancellationToken cancellationToken)
        {
            var result = await DownloadBlob(source, destination, SourceBlobOperation.Download, cancellationToken);
            return new DownloadBlobOutput
            {
                Directory = result.Directory,
                FileName = result.FileName,
                FullPath = result.FullPath
            };
        }

        /// <summary>
        ///     Reads blob content and returns it. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { string Content }</returns>
        public static async Task<ReadContentOutput> ReadBlobContentAsync(SourceProperties source,
            CancellationToken cancellationToken)
        {
            var result = await DownloadBlob(source, null, SourceBlobOperation.Read, cancellationToken);
            return new ReadContentOutput
            {
                Content = result.Content
            };
        }

        private static async Task<DownloadOutputBase> DownloadBlob(SourceProperties sourceProperties,
            DestinationFileProperties destinationProperties, SourceBlobOperation operation,
            CancellationToken cancellationToken)
        {
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();
            var container = Utils.GetBlobContainer(sourceProperties.ConnectionString, sourceProperties.ContainerName);
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();
            // get reference to blob
            var blobReference = Utils.GetCloudBlob(container, sourceProperties.BlobName, sourceProperties.BlobType);
            // check for interruptions
            cancellationToken.ThrowIfCancellationRequested();
            var encoding = blobReference.GetEncoding();
            var content = await blobReference.ReadContents(cancellationToken);
            switch (operation)
            {
                case SourceBlobOperation.Read:
                    return new DownloadOutputBase {Content = content};
                case SourceBlobOperation.Download:
                    return WriteToFile(content, sourceProperties.BlobName, encoding, destinationProperties);
                default:
                    throw new Exception("Unknown operations. Allowed operations are Read and Download");
            }
        }


        private static DownloadOutputBase WriteToFile(string content, string fileName, Encoding encoding,
            DestinationFileProperties destinationProperties)
        {
            var destinationFileName = fileName;
            if (File.Exists(Path.Combine(destinationProperties.Directory, destinationFileName)))
                switch (destinationProperties.FileExistsOperation)
                {
                    case FileExistsAction.Error:
                        throw new IOException($"Destination file '{destinationFileName}' already exists.");
                    case FileExistsAction.Rename:
                        destinationFileName =
                            Utils.GetRenamedFileName(destinationFileName, destinationProperties.Directory);
                        break;
                }

            // Write blob content to file
            var destinationFileFullPath = Path.Combine(destinationProperties.Directory, destinationFileName);
            File.WriteAllText(destinationFileFullPath, content, encoding);

            return new DownloadOutputBase
            {
                FullPath = destinationFileFullPath,
                Directory = Path.GetDirectoryName(destinationFileFullPath),
                FileName = Path.GetFileName(destinationFileFullPath)
            };
        }
    }

    public class DownloadOutputBase
    {
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string FullPath { get; set; }
        public string Content { get; set; }
    }

    public class DownloadBlobOutput
    {
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string FullPath { get; set; }
    }

    public class ReadContentOutput
    {
        public string Content { get; set; }
    }

    public class SourceProperties
    {
        /// <summary>
        ///     Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Name of the azure blob storage container where the file is downloaded from.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string ContainerName { get; set; }

        /// <summary>
        ///     Name of the blob to download
        /// </summary>
        [DefaultValue("example.xml")]
        [DisplayFormat(DataFormatString = "Text")]
        public string BlobName { get; set; }

        /// <summary>
        ///     Type of blob to download: Append, Block or Page.
        /// </summary>
        [DisplayName("Blob Type")]
        [DefaultValue(AzureBlobType.Block)]
        public AzureBlobType BlobType { get; set; }
    }

    public class DestinationFileProperties
    {
        /// <summary>
        ///     Download destination directory.
        /// </summary>
        [DefaultValue(@"c:\temp")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Directory { get; set; }

        /// <summary>
        ///     Error: Throws exception if destination file exists.
        ///     Rename: Adds '(1)' at the end of file name. Incerements the number if (1) already exists.
        ///     Overwrite: Overwrites existing file.
        /// </summary>
        [DefaultValue(FileExistsAction.Error)]
        public FileExistsAction FileExistsOperation { get; set; }
    }

    public enum FileExistsAction
    {
        Error,
        Rename,
        Overwrite
    }

    public enum SourceBlobOperation
    {
        Download,
        Read
    }
}