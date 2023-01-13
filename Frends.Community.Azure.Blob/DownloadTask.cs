using System;
using System.ComponentModel;
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
        /// <returns>Object { string FileName, string Directory, string FullPath, string OriginalFileName }</returns>
        public static async Task<DownloadBlobOutput> DownloadBlobAsync([PropertyTab]SourceProperties source,
            [PropertyTab]DestinationFileProperties destination, CancellationToken cancellationToken)
        {
            var blob = Utils.GetBlobClient(source.ConnectionMethod, source.ConnectionString, source.Connection, source.ContainerName, source.BlobName);
            string fullDestinationPath;
            string fileName;
            var originalFileName = destination.ParseIllegalCharacters ? source.BlobName : "";
            if (destination.ParseIllegalCharacters)
            {
                var parsedBlobName = HandleIllegalCharacters(source.BlobName);
                fullDestinationPath = Path.Combine(destination.Directory, parsedBlobName);
                fileName = parsedBlobName;
            }
            else
            {
                fullDestinationPath = Path.Combine(destination.Directory, source.BlobName);
                fileName = Path.GetFileNameWithoutExtension(fullDestinationPath);
            }

            var fileExtension = Path.HasExtension(fullDestinationPath) ? Path.GetExtension(fullDestinationPath) : "";

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
                FullPath = fullDestinationPath,
                OriginalFileName = originalFileName
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
                        sw.WriteLine(line);
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

        // Parse illegal characters from filename.
        private static string HandleIllegalCharacters(string fileName)
        {
            var invalid = new string(Path.GetInvalidFileNameChars());
            foreach (char character in invalid)
                fileName = fileName.Replace(character.ToString(), "");

            return fileName;
        }
    }
}