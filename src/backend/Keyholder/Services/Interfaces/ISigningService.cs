
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task Init();
    Task CreateTwinKeySignature(string deviceId);
}