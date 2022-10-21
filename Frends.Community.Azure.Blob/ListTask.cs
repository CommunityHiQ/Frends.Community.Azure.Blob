using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Threading;
using Azure.Identity;
using System;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class ListTask
    {
        /// <summary>
        ///     List blobs in container. See See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { List&lt;Object&gt; { string BlobType, string Uri, string Name, string ETag }}</returns>
        public static async Task<ListBlobsOutput> ListBlobs([PropertyTab]ListSourceProperties source, CancellationToken cancellationToken)
        {
            BlobContainerClient container;

            if (source.ConnectionMethod == ConnectionMethod.ConnectionString)
                container = Utils.GetBlobContainer(source.ConnectionString, source.ContainerName);
            else
                container = Utils.GetBlobContainer(source.Connection.ApplicationID, source.Connection.TenantID, source.Connection.ClientSecret, source.Connection.StorageAccountName, source.ContainerName);

            if (source.FlatBlobListing)
            {
                var enumerable = container.GetBlobsAsync(BlobTraits.None, BlobStates.None, string.IsNullOrWhiteSpace(source.Prefix) ? null : source.Prefix, cancellationToken).AsPages();
                var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
                return new ListBlobsOutput { Blobs = await ListBlobsFlat(enumerator, source, cancellationToken) };
            }
            else
            {
                var enumerable = container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", string.IsNullOrWhiteSpace(source.Prefix) ? null : source.Prefix, cancellationToken).AsPages();
                var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
                return new ListBlobsOutput { Blobs = await ListBlobsHierarchy(enumerator, source, container.Uri.ToString(), cancellationToken) };
            }
        }

        private static async Task<List<BlobData>> ListBlobsFlat(IAsyncEnumerator<Page<BlobItem>> enumerator, ListSourceProperties source, CancellationToken cancellationToken)
        {
            var blobs = new List<BlobData>();
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var blobItems = enumerator.Current;
                    foreach (var blobItem in blobItems.Values)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        BlobClient blob;

                        if (source.ConnectionMethod == ConnectionMethod.ConnectionString)
                            blob = new BlobClient(source.ConnectionString, source.ContainerName, blobItem.Name);
                        else
                        {
                            var credentials = new ClientSecretCredential(source.Connection.TenantID, source.Connection.ApplicationID, source.Connection.ClientSecret, new ClientSecretCredentialOptions());
                            var url = new Uri($"https://{source.Connection.StorageAccountName}.blob.core.windows.net/{source.ContainerName}/{blobItem.Name}");
                            blob = new BlobClient(url, credentials);
                        }

                        blobs.Add(new BlobData
                        {
                            BlobType = blobItem.Properties.BlobType.ToString(),
                            Uri = blob.Uri.ToString(),
                            Name = blob.Name,
                            ETag = blobItem.Properties.ETag.ToString()
                        });
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            return blobs;
        }

        private static async Task<List<BlobData>> ListBlobsHierarchy(IAsyncEnumerator<Page<BlobHierarchyItem>> enumerator, ListSourceProperties source, string uri, CancellationToken cancellationToken)
        {
            var blobs = new List<BlobData>();
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var blobItems = enumerator.Current;
                    foreach (var blobItem in blobItems.Values)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (blobItem.IsBlob)
                        {
                            BlobClient blob;

                            if (source.ConnectionMethod == ConnectionMethod.ConnectionString)
                                blob = new BlobClient(source.ConnectionString, source.ContainerName, blobItem.Blob.Name);
                            else
                            {
                                var credentials = new ClientSecretCredential(source.Connection.TenantID, source.Connection.ApplicationID, source.Connection.ClientSecret, new ClientSecretCredentialOptions());
                                var url = new Uri($"https://{source.Connection.StorageAccountName}.blob.core.windows.net/{source.ContainerName}/{blobItem.Blob.Name}");
                                blob = new BlobClient(url, credentials);
                            }

                            blobs.Add(new BlobData
                            {
                                BlobType = blobItem.Blob.Properties.BlobType.ToString(),
                                Uri = blob.Uri.ToString(),
                                Name = blob.Name,
                                ETag = blobItem.Blob.Properties.ETag.ToString()
                            });
                        }
                        else
                        {
                            blobs.Add(new BlobData
                            {
                                BlobType = "Directory",
                                Uri = uri + "/" + blobItem.Prefix,
                                Name = blobItem.Prefix,
                                ETag = null
                            });
                        }
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            return blobs;
        }
    }

    public class ListSourceProperties
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
        ///     Specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.
        /// </summary>
        [DefaultValue("true")]
        public bool FlatBlobListing { get; set; }

        /// <summary>
        ///     Blob prefix used while searching container
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string Prefix { get; set; }
    }

    public class ListBlobsOutput
    {
        public List<BlobData> Blobs { get; set; }
    }

    public class BlobData
    {
        public string BlobType { get; set; }
        public string Uri { get; set; }
        public string Name { get; set; }
        public string ETag { get; set; }
    }
}