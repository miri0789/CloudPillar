public static class WebApplicationExtensions
{
    private const string AUTHENTICATION_X509 = "X509";
    private const string AUTHENTICATION_SAS = "SAS";

    public static void ValidateAuthenticationSettings(this WebApplication webApp)
    {
        var appSettings = new AppSettings();
        webApp.Configuration.GetSection("AppSettings").Bind(appSettings); 
        
        if (!appSettings.StrictMode)
        {
            return;
        }

        if (!appSettings.PermanentAuthentucationMethods.Equals(AUTHENTICATION_X509))
        {
            throw new Exception($"PermanentAuthentucationMethods value must be X509. The value {appSettings.PermanentAuthentucationMethods} is not valid");
        }
        if (!appSettings.ProvisionalAuthentucationMethods.Equals(AUTHENTICATION_SAS))
        {
            throw new Exception($"ProvisionalAuthentucationMethods value must be SAS. The value {appSettings.ProvisionalAuthentucationMethods} is not valid");
        }
    }
}
