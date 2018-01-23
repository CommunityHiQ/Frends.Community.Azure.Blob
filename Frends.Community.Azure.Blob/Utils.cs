using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.IO;

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
            while(File.Exists(Path.Combine(directory, $"{name}({index}){extension}")))
            {
                index++;
            }
            return $"{name}({index}){extension}";
        }
    }
}
