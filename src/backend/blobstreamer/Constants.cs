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
        #endregion
    }
}