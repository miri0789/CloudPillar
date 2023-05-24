namespace blobstreamer
{
    public static class Constants
    {
        #region envirementVariables
        public static readonly string storageConnectionString = "STORAGE_CONNECTION_STRING";
        public static readonly string blobContainerName = "BLOB_CONTAINER_NAME";
        public static readonly string iothubConnectionString = "IOTHUB_CONNECTION_STRING";
        #endregion

        #region appProperties
        public static readonly string messageExpiredMinutes = "MESSAGE_EXPIRED_MINUTES";
        public static readonly string retryPolicyBaseDelay = "RETRY_POLICY_BASE_DELAY";
        public static readonly string retryPolicyExponent = "RETRY_POLICY_EXPONENT";

        #endregion
    }
}