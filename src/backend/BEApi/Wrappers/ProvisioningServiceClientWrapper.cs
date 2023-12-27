using Backend.BEApi.Wrappers.Interfaces;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace Backend.BEApi.Wrappers;

public class ProvisioningServiceClientWrapper : IProvisioningServiceClientWrapper
{
    public ProvisioningServiceClient Create(string connectionString)
    {
        return ProvisioningServiceClient.CreateFromConnectionString(connectionString);
    }

    public async Task<IndividualEnrollment> CreateOrUpdateIndividualEnrollmentAsync(ProvisioningServiceClient provisioningServiceClient, IndividualEnrollment enrollment)
    {
        return await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(enrollment);
    }

    public async Task DeleteIndividualEnrollmentAsync(ProvisioningServiceClient provisioningServiceClient, string enrollmentId)
    {
        await provisioningServiceClient.DeleteIndividualEnrollmentAsync(enrollmentId);
    }
}