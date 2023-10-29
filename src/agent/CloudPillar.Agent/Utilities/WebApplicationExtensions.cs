public static class WebApplicationExtensions
{
    private const AuthenticationMethod AUTHENTICATION_X509 = AuthenticationMethod.X509;
    private const AuthenticationMethod AUTHENTICATION_SAS = AuthenticationMethod.SAS;
    private const string APP_SETTINGS_SECTION = "AppSettings";

    public static void ValidateAuthenticationSettings(this WebApplication webApp)
    {
        var appSettings = new AppSettings();
        webApp.Configuration.GetSection(APP_SETTINGS_SECTION).Bind(appSettings); 
        
        if (!appSettings.StrictMode)
        {
            return;
        }

        if (!appSettings.PermanentAuthentucationMethods.Equals(AUTHENTICATION_X509.ToString()))
        {
            throw new ArgumentException($"PermanentAuthentucationMethods value must be X509. The value {appSettings.PermanentAuthentucationMethods} is not valid");
        }
        if (!appSettings.ProvisionalAuthentucationMethods.Equals(AUTHENTICATION_SAS.ToString()))
        {
            throw new ArgumentException($"ProvisionalAuthentucationMethods value must be SAS. The value {appSettings.ProvisionalAuthentucationMethods} is not valid");
        }
    }
}
