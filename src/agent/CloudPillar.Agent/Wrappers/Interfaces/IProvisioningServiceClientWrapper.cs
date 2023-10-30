using Microsoft.Azure.Devices.Provisioning.Service;

namespace CloudPillar.Agent.Wrappers;
public interface IProvisioningServiceClientWrapper
{
    Task<IndividualEnrollment> GetIndividualEnrollmentAsync(string connectionString, string registrationId, CancellationToken cancellationToken);
}