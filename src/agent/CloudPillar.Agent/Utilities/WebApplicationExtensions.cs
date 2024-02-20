using CloudPillar.Agent.Enums;

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

        if (strictModeSettings.PermanentAuthenticationMethods?.Equals(AuthenticationMethod.X509.ToString()) == false)
        {
            throw new ArgumentException($"PermanentAuthenticationMethods value must be X509. The value {strictModeSettings.PermanentAuthenticationMethods} is not valid");
        }
        if (strictModeSettings.ProvisionalAuthenticationMethods?.Equals(AuthenticationMethod.SAS.ToString()) == false)
        {
            throw new ArgumentException($"ProvisionalAuthenticationMethods value must be SAS. The value {strictModeSettings.ProvisionalAuthenticationMethods} is not valid");
        }
    }
}
