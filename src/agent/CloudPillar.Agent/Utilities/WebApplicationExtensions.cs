public static class WebApplicationExtensions
{
    public const string STRICT_MODE_SETTINGS_SECTION = "StrictModeSettings";

    public static void ValidateAuthenticationSettings(this WebApplication webApp)
    {
        var strictModeSettings = new StrictModeSettings();
        webApp.Configuration.GetSection(STRICT_MODE_SETTINGS_SECTION).Bind(strictModeSettings); 
        
        if (!strictModeSettings.StrictMode)
        {
            return;
        }

        if (!strictModeSettings.PermanentAuthentucationMethods.Equals(AuthenticationMethod.X509.ToString()))
        {
            throw new ArgumentException($"PermanentAuthentucationMethods value must be X509. The value {strictModeSettings.PermanentAuthentucationMethods} is not valid");
        }
        if (!strictModeSettings.ProvisionalAuthentucationMethods.Equals(AuthenticationMethod.SAS.ToString()))
        {
            throw new ArgumentException($"ProvisionalAuthentucationMethods value must be SAS. The value {strictModeSettings.ProvisionalAuthentucationMethods} is not valid");
        }
    }
}
