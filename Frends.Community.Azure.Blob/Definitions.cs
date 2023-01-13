#pragma warning disable 1591

using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Frends.Community.Azure.Blob
{
    public class OAuthConnection
    {
        /// <summary>
        ///     Application (Client) ID of Azure AD Application.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DisplayName("Application ID")]
        public string ApplicationID { get; set; }

        /// <summary>
        ///     Tenant ID of Azure Tenant.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DisplayName("Tenant ID")]
        public string TenantID { get; set; }

        /// <summary>
        ///     Client Secret of Azure AD Application.
        /// </summary>
        [PasswordPropertyText]
        [DisplayName("Client Secret")]
        public string ClientSecret { get; set; }

        /// <summary>
        ///     Name of the storage account.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DisplayName("Storage Account Name")]
        public string StorageAccountName { get; set; }
    }

    public class DownloadBlobOutput
    {
        /// <summary>
        ///     FileName of the downloaded Blob.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///     DIrectory where the Blob was Downloaded.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        ///  Full path to the downloaded file.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        ///     If illegal characters were parsed, original name of the Blob.
        ///     Otherwise empty.
        /// </summary>
        public string OriginalFileName { get; set; }
    }

    public class ReadContentOutput
    {
        /// <summary>
        ///     Content of the Blob.
        /// </summary>
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
        [DisplayFormat(DataFormatString = "Text")]
        [PasswordPropertyText]
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

        /// <summary>
        ///     If Blob name contains illegal characters, should they be removed?
        /// </summary>
        [DefaultValue(false)]
        public bool ParseIllegalCharacters { get; set; }
    }

    public enum FileExistsAction
    {
        /// <summary>
        ///     Throw error.
        /// </summary>
        Error,
        /// <summary>
        ///     Rename the new file.
        ///     If file "test.txt" exists, new file will be named as "test(1).txt"
        /// </summary>
        Rename,
        /// <summary>
        ///     Overwrite the existing file.
        /// </summary>
        Overwrite
    }

    public enum ConnectionMethod
    {
        /// <summary>
        ///     Use connection string as connection method.
        /// </summary>
        ConnectionString,
        /// <summary>
        ///     Use access token authentication as connection method.
        /// </summary>
        OAuth2
    }
}
