﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class Utils
    {
        public static BlobContainerClient GetBlobContainer(string connectionString, string containerName)
        {
            // initialize azure account
            var blobServiceClient = new BlobServiceClient(connectionString);

            // Fetch the container client
            return blobServiceClient.GetBlobContainerClient(containerName);
        }

        public static BlobContainerClient GetBlobContainer(string appID, string tenantID, string clientSecret, string storageAccount, string containerName)
        {
            var credentials = new ClientSecretCredential(tenantID, appID, clientSecret, new ClientSecretCredentialOptions());

            // initialize azure account
            var blobServiceClient = new BlobServiceClient(new Uri($"https://{storageAccount}.blob.core.windows.net"), credentials);

            // Fetch the container client
            return blobServiceClient.GetBlobContainerClient(containerName);
        }

        public static BlobClient GetBlobClient(ConnectionMethod method, string connectionString, OAuthConnection connection, string containerName, string blobName)
        {
            if (method == ConnectionMethod.ConnectionString)
                return new BlobClient(connectionString, containerName, blobName);
            else
            {
                var credentials = new ClientSecretCredential(connection.TenantID, connection.ApplicationID, connection.ClientSecret, new ClientSecretCredentialOptions());
                var url = new Uri($"https://{connection.StorageAccountName}.blob.core.windows.net/{containerName}/{blobName}");
                return new BlobClient(url, credentials);
            }
        }

        public static AppendBlobClient GetAppendBlobClient(ConnectionMethod method, string connectionString, OAuthConnection connection, string containerName, string blobName)
        {
            if (method == ConnectionMethod.ConnectionString)
                return new AppendBlobClient(connectionString, containerName, blobName);
            else
            {
                var credentials = new ClientSecretCredential(connection.TenantID, connection.ApplicationID, connection.ClientSecret, new ClientSecretCredentialOptions());
                var url = new Uri($"https://{connection.StorageAccountName}.blob.core.windows.net/{containerName}/{blobName}");
                return new AppendBlobClient(url, credentials);
            }
        }

        public static PageBlobClient GetPageBlobClient(ConnectionMethod method, string connectionString, OAuthConnection connection, string containerName, string blobName)
        {
            if (method == ConnectionMethod.ConnectionString)
                return new PageBlobClient(connectionString, containerName, blobName);
            else
            {
                var credentials = new ClientSecretCredential(connection.TenantID, connection.ApplicationID, connection.ClientSecret, new ClientSecretCredentialOptions());
                var url = new Uri($"https://{connection.StorageAccountName}.blob.core.windows.net/{containerName}/{blobName}");
                return new PageBlobClient(url, credentials);
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