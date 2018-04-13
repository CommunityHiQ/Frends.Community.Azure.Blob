using Frends.Tasks.Attributes;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.ComponentModel;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public class ListTask
    {
        /// <summary>
        /// List blobs in container. See See https://github.com/CommunityHiQ/Frends.Community.Azure.Blob
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { List&lt;Object&gt; { string Name, string Uri, string BlobType }}</returns>
        public static ListBlobsOutput ListBlobs(ListSourceProperties source)
        {
            var container = Utils.GetBlobContainer(source.ConnectionString, source.ContainerName);

            var blobs = new List<BlobData>();

            foreach(IListBlobItem item in container.ListBlobs(String.IsNullOrWhiteSpace(source.Prefix) ? null : source.Prefix, source.FlatBlobListing))
            {
                var blobType = item.GetType();
                
                if(blobType == typeof(CloudBlockBlob))
                {
                    var blockBlob = (CloudBlockBlob)item;
                    blobs.Add(new BlobData { BlobType = "Block", Uri = blockBlob.Uri.ToString(), Name = blockBlob.Name });

                }else if (blobType == typeof(CloudPageBlob))
                {
                    var pageBlob = (CloudPageBlob)item;
                    blobs.Add(new BlobData { BlobType = "Page", Uri = pageBlob.Uri.ToString(), Name = pageBlob.Name });

                }else if(blobType == typeof(CloudBlobDirectory))
                {
                    var directory = (CloudBlobDirectory)item;
                    blobs.Add(new BlobData { BlobType = "Directory", Uri = directory.Uri.ToString(), Name = directory.Prefix });
                }
            }

            return new ListBlobsOutput { Blobs = blobs };
        }
    }
    public class ListSourceProperties
    {
        /// <summary>
        /// Connection string to Azure storage
        /// </summary>
        [DefaultValue("UseDevelopmentStorage=true")]
        [DefaultDisplayType(DisplayType.Text)]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Name of the azure blob storage container where the file is downloaded from.
        /// </summary>
        [DefaultDisplayType(DisplayType.Text)]
        public string ContainerName { get; set; }

        /// <summary>
        /// Specifies whether to list blobs in a flat listing, or whether to list blobs hierarchically, by virtual directory.
        /// </summary>
        [DefaultValue("true")]
        public bool FlatBlobListing { get; set; }

        /// <summary>
        /// Blob prefix used while searching container
        /// </summary>
        [DefaultDisplayType(DisplayType.Text)]
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
    }
}
