public class Constants
{
    public const string X_DEVICE_ID = "X-device-id";
    public const string X_SECRET_KEY = "X-secret-key";
    public const int HTTP_DEFAULT_PORT = 8099;
    public const string CONFIG_PORT = "Port";
    public const int DIAGNOSTICS_FILE_SIZE_KB = 128;
    public static readonly string DIAGNOSTICS_FILE_PATH;
    static Constants()
    {
        DIAGNOSTICS_FILE_PATH = AppDomain.CurrentDomain.BaseDirectory;
    }

}