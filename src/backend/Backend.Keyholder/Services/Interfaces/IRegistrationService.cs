public interface IRegistrationService
{
    Task Register(string deviceName, string OneMDKey, string iotHubHostName);
}