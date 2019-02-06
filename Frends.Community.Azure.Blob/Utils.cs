using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class Utils
    {
        public static CloudBlobContainer GetBlobContainer(string connectionString, string containerName)
        {
            // initialize azure account
            var account = CloudStorageAccount.Parse(connectionString);

            // initialize blob client
            var client = account.CreateCloudBlobClient();

            return client.GetContainerReference(containerName);
        }

        public static CloudBlob GetCloudBlob(CloudBlobContainer container, string blobName, AzureBlobType blobType)
        {
            switch (blobType)
            {
                case AzureBlobType.Append:
                    return container.GetAppendBlobReference(blobName);
                case AzureBlobType.Block:
                    return container.GetBlockBlobReference(blobName);
                case AzureBlobType.Page:
                    return container.GetPageBlobReference(blobName);
                default:
                    return container.GetBlockBlobReference(blobName);
            }
        }

        public static string GetRenamedFileName(string fileName, string directory)
        {
            // if fileName is available, just return that
            if (!File.Exists(Path.Combine(directory, fileName)))
                return fileName;

            var index = 1;
            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            // loop until available indexed filename found
            while (File.Exists(Path.Combine(directory, $"{name}({index}){extension}"))) index++;

            return $"{name}({index}){extension}";
        }

        /// <summary>
        ///     Gets correct stream object. Does not always dispose, so use using.
        /// </summary>
        /// <param name="compress"></param>
        /// <param name="file"></param>
        /// <param name="fromString"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static Stream GetStream(bool compress, bool fromString, Encoding encoding, FileInfo file)
        {
            var fileStream = File.OpenRead(file.FullName);

            if (!compress && !fromString)
                return fileStream; // as uncompressed binary

            byte[] bytes;
            if (!compress)
            {
                using (var reader = new StreamReader(fileStream, encoding))
                {
                    bytes = encoding.GetBytes(reader.ReadToEnd());
                }
                return new MemoryStream(bytes); // as uncompressed string
            }

            using (var outStream = new MemoryStream())
            {
                using (var gzip = new GZipStream(outStream, CompressionMode.Compress))
                {
                    if (!fromString)
                        fileStream.CopyTo(gzip); // as compressed binary
                    else
                        using (var reader = new StreamReader(fileStream, encoding))
                        {
                            var content = reader.ReadToEnd();
                            using (var encodedMemory = new MemoryStream(encoding.GetBytes(content)))
                            {
                                encodedMemory.CopyTo(gzip); // as compressed string
                            }
                        }
                }
                bytes = outStream.ToArray();
            }
            fileStream.Dispose();

            var memStream = new MemoryStream(bytes);
            return memStream;
        }
    }
}