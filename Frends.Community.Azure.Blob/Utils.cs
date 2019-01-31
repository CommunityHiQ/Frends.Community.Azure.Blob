using System;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class Utils
    {
        public static CloudBlobContainer GetBlobContainer(string connectionString, string containerName)
        {
            // initialize azure account
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);

            // initialize blob client
            CloudBlobClient client = account.CreateCloudBlobClient();

            return client.GetContainerReference(containerName);
        }

        public static CloudBlob GetCloudBlob(CloudBlobContainer container, string blobName, AzureBlobType blobType)
        {
            CloudBlob cloudBlob;

            switch (blobType)
            {
                case AzureBlobType.Append:
                    cloudBlob = container.GetAppendBlobReference(blobName);
                    break;
                case AzureBlobType.Block:
                    cloudBlob = container.GetBlockBlobReference(blobName);
                    break;
                case AzureBlobType.Page:
                    cloudBlob = container.GetPageBlobReference(blobName);
                    break;
                default:
                    cloudBlob = container.GetBlockBlobReference(blobName);
                    break;
            }

            return cloudBlob;
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
            while (File.Exists(Path.Combine(directory, $"{name}({index}){extension}")))
            {
                index++;
            }

            return $"{name}({index}){extension}";
        }

        /// <summary>
        /// Gets correct stream object. Does not dispose, so use using.
        /// </summary>
        /// <param name="compress"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Stream GetStream(bool compress, bool fromString, Encoding encoding, FileInfo file)
        {
            byte[] compressed;
            var fileStream = File.OpenRead(file.FullName);
            {

                if (!compress && !fromString)
                    return fileStream; // as uncompressed binary

                if (!compress)
                {
                    byte[] bytes;
                    using (var reader = new StreamReader(fileStream, encoding))
                    {
                        
                        bytes = encoding.GetBytes(reader.ReadToEnd());
                        
                    }

                    fileStream.Dispose();
                    return new MemoryStream(bytes); // as uncompressed string
                }

                using (var outStream = new MemoryStream())
                {
                    using (var gzip = new GZipStream(outStream, CompressionMode.Compress))
                    {
                        if (fromString)
                        {
                            using (var encodedMemory = new MemoryStream(
                                encoding.GetBytes(new StreamReader(fileStream, encoding).ReadToEnd())))
                            {
                                encodedMemory.CopyTo(gzip); // as compressed string
                            }
                        }
                        else
                        {
                            fileStream.CopyTo(gzip); // as compressed binary
                        }
                    }

                    compressed = outStream.ToArray();
                }
            }
            fileStream.Dispose();

            var memStream = new MemoryStream(compressed);
            return memStream;
        }
    }
}
