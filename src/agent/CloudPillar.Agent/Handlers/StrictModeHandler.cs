using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    private readonly AppSettings _appSettings;
    public StrictModeHandler(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }
    public void CheckAuthentucationMethodValue()
    {
        if (!_appSettings.PermanentAuthentucationMethods.Equals(AuthenticationConstants.AUTHENTICATION_X509) ||
        !_appSettings.ProvisionalAuthentucationMethods.Equals(AuthenticationConstants.AUTHENTICATION_SAS))
            throw new InvalidOperationException("Authentucation Methods configuration is not valid");
    }
}