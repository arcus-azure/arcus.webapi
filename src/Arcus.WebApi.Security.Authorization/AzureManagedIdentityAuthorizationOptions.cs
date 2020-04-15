using GuardNet;

namespace Arcus.WebApi.Security.Authorization
{
    public class AzureManagedIdentityAuthorizationOptions
    {
        private const string DefaultHeaderName = "x-managed-identity-token";

        public string HeaderName { get; set; }

        public AzureManagedIdentityAuthorizationOptions(string headerName)
        {
            Guard.NotNullOrEmpty(headerName,nameof(headerName));

            HeaderName = headerName;
        }

        public static readonly AzureManagedIdentityAuthorizationOptions Default = new AzureManagedIdentityAuthorizationOptions(DefaultHeaderName);
    }
}
