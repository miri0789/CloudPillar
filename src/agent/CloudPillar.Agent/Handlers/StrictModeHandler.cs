using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    private readonly AppSettings _appSettings;
    public const string AUTHENTICATION_SAS = "SAS";
    public const string AUTHENTICATION_X509 = "X509";

    public StrictModeHandler(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }
    public void CheckAuthentucationMethodValue()
    {
        if (_appSettings.StrictMode == false) { return; }

        if (!_appSettings.PermanentAuthentucationMethods.Equals(AUTHENTICATION_X509))
        {
            throw new InvalidOperationException($"PermanentAuthentucationMethods value in appSettings.json must be X509, The value {_appSettings.PermanentAuthentucationMethods} is not valid");
        }
        if (!_appSettings.ProvisionalAuthentucationMethods.Equals(AUTHENTICATION_SAS))
        {
            throw new InvalidOperationException($"ProvisionalAuthentucationMethods value in appSettings.json must be SAS, The value {_appSettings.ProvisionalAuthentucationMethods} is not valid");
        }
    }
}