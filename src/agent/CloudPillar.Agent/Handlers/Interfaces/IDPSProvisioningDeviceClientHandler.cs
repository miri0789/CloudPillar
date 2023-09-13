
namespace CloudPillar.Agent.Handlers;
public interface IDPSProvisioningDeviceClientHandler
{
    Task Provisioning(string spcScopeId, string certificateThumbprint);

    Task<bool> Authentication
}