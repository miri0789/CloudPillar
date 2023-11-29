namespace Backend.Infra.Wrappers;
public class CommonEnvironmentsWrapper: ICommonEnvironmentsWrapper
{
    private const string _retryPolicyBaseDelay = "RetryPolicyBaseDelay";
    private const string _retryPolicyExponent = "RetryPolicyExponent";

    public int retryPolicyBaseDelay
    {
        get
        {
            return int.TryParse(GetVariable(_retryPolicyBaseDelay), out int value) ? value : 1;
        }
    }
    public int retryPolicyExponent
    {
        get
        {
            return int.TryParse(GetVariable(_retryPolicyExponent), out int value) ? value : 3;
        }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
