using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;

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
        public static async Task<DownloadBlobOutput> DownloadBlobAsync([PropertyTab]SourceProperties source,
            [PropertyTab]DestinationFileProperties destination, CancellationToken cancellationToken)
        {
            var blob = Utils.GetBlobClient(source.ConnectionMethod, source.ConnectionString, source.Connection, source.ContainerName, source.BlobName);

            var fullDestinationPath = Path.Combine(destination.Directory, source.BlobName);
            var fileName = source.BlobName.Split('.')[0];
            var fileExtension = "";
            if (source.BlobName.Split('.').Length > 1)
            {
                fileName = string.Join(".", source.BlobName.Split('.').Take(source.BlobName.Split('.').Length - 1).ToArray());
                fileExtension = "." + source.BlobName.Split('.').Last();
            }

            if (destination.FileExistsOperation == FileExistsAction.Error && File.Exists(fullDestinationPath))
                throw new IOException("File already exists in destination path. Please delete the existing file or change the \"file exists operation\" to OverWrite.");

            if (destination.FileExistsOperation == FileExistsAction.Rename && File.Exists(fullDestinationPath))
            {
                var increment = 1;
                var incrementedFileName = fileName + "(" + increment.ToString() + ")" + fileExtension;
                while (File.Exists(Path.Combine(destination.Directory, incrementedFileName)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    increment++;
                    incrementedFileName = fileName + "(" + increment.ToString() + ")" + fileExtension;
                }
                fullDestinationPath = Path.Combine(destination.Directory, incrementedFileName);
                fileName = incrementedFileName;
                await blob.DownloadToAsync(fullDestinationPath, cancellationToken);
            }
            else
                await blob.DownloadToAsync(fullDestinationPath, cancellationToken);

            CheckAndFixFileEncoding(fullDestinationPath, destination.Directory, fileExtension, source.Encoding);
            return new DownloadBlobOutput
            {
                Directory = destination.Directory,
                FileName = fileName,
                FullPath = fullDestinationPath
            };
        }

        /// <summary>
        ///     Reads blob content and returns it. See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { string Content }</returns>
        public static async Task<ReadContentOutput> ReadBlobContentAsync([PropertyTab]SourceProperties source,
            CancellationToken cancellationToken)
        {
            var blob = Utils.GetBlobClient(source.ConnectionMethod, source.ConnectionString, source.Connection, source.ContainerName, source.BlobName); ;

            var result = await blob.DownloadContentAsync(cancellationToken);
            return new ReadContentOutput
            {
                Content = SetStringEncoding(result.Value.Content.ToString(), source.Encoding)
            };
        }

        /// <summary>
        ///     Check if the file encoding matches with given encoding and fix the encoding if it doesn't match.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="directory"></param>
        /// <param name="fileExtension"></param>
        /// <param name="targetEncoding"></param>
        /// <returns></returns>
        private static void CheckAndFixFileEncoding(string fullPath, string directory, string fileExtension, string targetEncoding)
        {
            var encoding = "";
            using (var reader = new StreamReader(fullPath, true))
            {
                reader.Read();
                encoding = reader.CurrentEncoding.BodyName;
            }
            if (targetEncoding.ToLower() != encoding)
            {
                Encoding newEncoding;
                try
                {
                    newEncoding = Encoding.GetEncoding(targetEncoding.ToLower());
                }
                catch (Exception)
                {
                    throw new Exception("Provided encoding is not supported. Please check supported encodings from Encoding-option.");
                }
                var tempFilePath = Path.Combine(directory, "encodingTemp" + fileExtension);
                using (var sr = new StreamReader(fullPath, true))
                using (var sw = new StreamWriter(tempFilePath, false, newEncoding))
                {
                    var line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        sw.WriteLine(line);
                    }
                }
                File.Delete(fullPath);
                File.Copy(tempFilePath, fullPath);
                File.Delete(tempFilePath);
            }
        }

        /// <summary>
        ///     Set encoding of string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static string SetStringEncoding(string text, string encoding)
        {
            var bytes = Encoding.Default.GetBytes(text);

            switch (encoding.ToLower())
            {
                case "utf-8":
                    return Encoding.UTF8.GetString(bytes);
                case "utf-7":
                    return Encoding.UTF7.GetString(bytes);
                case "utf-32":
                    return Encoding.UTF32.GetString(bytes);
                case "unicode":
                    return Encoding.Unicode.GetString(bytes);
                case "ascii":
                    return Encoding.ASCII.GetString(bytes);
                default:
                    throw new Exception("Provided encoding is not supported. Please check supported encodings from Encoding-option.");
            }
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
        ///     Which connection method should be used for connecting to Azure Blob Storage?
        /// </summary>
        [DefaultValue(ConnectionMethod.ConnectionString)]
        public ConnectionMethod ConnectionMethod { get; set; }

        /// <summary>
        ///     Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DisplayFormat(DataFormatString = "Text")]
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ConnectionString)]
        public string ConnectionString { get; set; }

        /// <summary>
        ///     OAuth2 connection information.
        /// </summary>
        [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.OAuth2)]
        public OAuthConnection Connection { get; set; }

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
        ///     Azure blob type to upload: Append, Block or Page
        /// </summary>
        [DefaultValue(AzureBlobType.Block)]
        [DisplayName("Blob Type")]
        public AzureBlobType BlobType { get; set; }

        /// <summary>
        ///     Set encoding manually. Empty value tries to get encoding set in Azure.
        ///     Supported values are utf-8, utf-7, utf-32, unicode, bigendianunicode and ascii.
        /// </summary>
        [DefaultValue("UTF-8")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Encoding { get; set; }
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