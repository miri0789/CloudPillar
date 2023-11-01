using Microsoft.Azure.Devices.Provisioning.Service;

namespace CloudPillar.Agent.Wrappers;
public class ProvisioningServiceClientWrapper : IProvisioningServiceClientWrapper
{
    public async Task<IndividualEnrollment> GetIndividualEnrollmentAsync(string connectionString, string registrationId, CancellationToken cancellationToken)
    {
        using (ProvisioningServiceClient provisioningServiceClient =
                            ProvisioningServiceClient.CreateFromConnectionString(connectionString))
        {
            return await provisioningServiceClient.GetIndividualEnrollmentAsync(registrationId, cancellationToken);
        }
    }
}