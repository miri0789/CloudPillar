namespace CloudPillar.Agent.Wrappers;

public interface IConfigurationWrapper
{
    int GetValue(string key, int defaultValue);
}