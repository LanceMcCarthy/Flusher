namespace Flusher.Common.Helpers
{
    public static class Secrets
    {
		// See https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-3.1
        public static string WebHomePageUrl = "YOUR_ASPNET_ROOT_URL";
        public static string SignalRServerEndpoint = "YOUR_ASPNET_ROOT_URL/YOUR_SIGNALR_HUB_ROUTE";
		
        // See https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-rest-api
        public static string AzureStorageRootUrl = "YOUR_AZURE_BLOB_ROOT";
        public static string BlobConnectionString = "YOUR_AZURE-BLOB-CONNECTIONSTRING";
        public static string BlobContainerName = "AZURE_BLOB_CONTAINER_NAME";

        // See https://www.customvision.ai/
        public static string CustomVisionApiKey = "AZURE_CUSTOM_VISION_API_KEY";

        // If you want to keep this get an API key here https://sendgrid.com/docs/API_Reference/Web_API_v3/API_Keys/index.html
        public static string SendGridApiKey = "SENDGRID_API_KEY_FOR_EMAIL";
        public static string EmailAddressOne = "EMAIL_ADDRESS_FOR_ADMIN_ONE";
        public static string EmailAddressOneName = "NAME_OF_ADMIN_ONE";
        public static string EmailAddressTwo = "EMAIL_ADDRESS_FOR_ADMIN_TWO";
        public static string EmailAddressTwoName = "NAME_OF_ADMIN_TWO";
    }
}
