using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class ListTask
    {
        /// <summary>
        ///     List blobs in container. See See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Object { List&lt;Object&gt; { string BlobType, string Uri, string Name, string ETag }}</returns>
        public static async Task<ListBlobsOutput> ListBlobs(ListSourceProperties source)
        {
            var container = Utils.GetBlobContainer(source.ConnectionString, source.ContainerName);
            if (source.FlatBlobListing)
            {
                var enumerable = container.GetBlobsAsync(BlobTraits.None, BlobStates.None, string.IsNullOrWhiteSpace(source.Prefix) ? null : source.Prefix).AsPages();
                var enumerator = enumerable.GetAsyncEnumerator();
                return new ListBlobsOutput { Blobs = await ListBlobsFlat(enumerator, source) };
            }
            else
            {
                var enumerable = container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", string.IsNullOrWhiteSpace(source.Prefix) ? null : source.Prefix).AsPages();
                var enumerator = enumerable.GetAsyncEnumerator();
                return new ListBlobsOutput { Blobs = await ListBlobsHierarchy(enumerator, source, container.Uri.ToString()) };
            }
        }

        private static async Task<List<BlobData>> ListBlobsFlat(IAsyncEnumerator<Page<BlobItem>> enumerator, ListSourceProperties source)
        {
            var blobs = new List<BlobData>();
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var blobItems = enumerator.Current;
                    foreach (var blobItem in blobItems.Values)
                    {
                        var blob = new BlobClient(source.ConnectionString, source.ContainerName, blobItem.Name);
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

        private static async Task<List<BlobData>> ListBlobsHierarchy(IAsyncEnumerator<Page<BlobHierarchyItem>> enumerator, ListSourceProperties source, string uri)
        {
            var blobs = new List<BlobData>();
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var blobItems = enumerator.Current;
                    foreach (var blobItem in blobItems.Values)
                    {
                        if (blobItem.IsBlob)
                        {
                            var blob = new BlobClient(source.ConnectionString, source.ContainerName, blobItem.Blob.Name);
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