public interface IRegistrationService
{
    Task Register(string deviceId, string OneMDKey, string iotHubHostName, string password);
}