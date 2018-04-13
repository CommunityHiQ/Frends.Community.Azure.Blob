using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591

namespace Frends.Community.Azure.Blob
{
    public static class Extensions
    {
        public static Encoding ConvertToEncoding(this string value)
        {
            try
            {
                // if no value given, return os default encoding
                if (string.IsNullOrWhiteSpace(value))
                    return Encoding.Default;
                // check if encoding is given as code page
                int valueInt = 0;
                if (int.TryParse(value, out valueInt))
                    return Encoding.GetEncoding(valueInt);

                // otherwise encoding name is given
                return Encoding.GetEncoding(value);
            }
            catch
            {
                return Encoding.Default;
            }
        }

        /// <summary>
        /// Returns blob encoding if set, otherwise returns system default encoding
        /// </summary>
        /// <param name="blob"></param>
        /// <returns>Blob encoding</returns>
        public static Encoding GetEncoding(this CloudBlob blob)
        {
            if (blob.Properties == null || string.IsNullOrEmpty(blob.Properties.ContentEncoding))
                return Encoding.Default;

            return blob.Properties.ContentEncoding.ConvertToEncoding();
        }

        /// <summary>
        /// Reads blob content to string
        /// </summary>
        /// <param name="blobReference"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Blob content</returns>
        public static async Task<string> ReadContents(this CloudBlob blobReference, CancellationToken cancellationToken)
        {
            using (var blobStream = new MemoryStream())
            {
                using (var outputStream = new MemoryStream())
                {
                    await blobReference.DownloadToStreamAsync(blobStream, cancellationToken);
                    blobStream.Seek(0, SeekOrigin.Begin);
                    Encoding encoding = blobReference.GetEncoding();

                    if (blobReference.IsGZipped())
                        using (var gzipStream = new GZipStream(blobStream, CompressionMode.Decompress))
                            gzipStream.CopyTo(outputStream);
                    else
                        blobStream.CopyTo(outputStream);
                    
                    return encoding.GetString(outputStream.ToArray());
                }
            }
        }

        public static bool IsGZipped(this CloudBlob cloudBlob)
        {
            return cloudBlob.Properties != null && cloudBlob.Properties.ContentEncoding != null && cloudBlob.Properties.ContentEncoding.Equals("gzip", System.StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
