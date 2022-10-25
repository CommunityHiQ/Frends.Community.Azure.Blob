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
    public enum ConnectionMethod
    {
        ConnectionString,
        OAuth2
    }
}
