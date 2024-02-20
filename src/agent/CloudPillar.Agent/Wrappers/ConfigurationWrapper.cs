namespace CloudPillar.Agent.Wrappers;

public class ConfigurationWrapper : IConfigurationWrapper
{
    private readonly IConfiguration _configuration;

    public ConfigurationWrapper(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public int GetValue(string key, int defaultValue)
    {
        return _configuration.GetValue(key, defaultValue);
    }

    public bool GetValue(string key, bool defaultValue)
    {
        return _configuration.GetValue(key, defaultValue);
    }
}