using Microsoft.Azure.Devices.Provisioning.Service;

namespace Backend.Keyholder.Wrappers.Interfaces;
public interface IProvisioningServiceClientWrapper
{
    ProvisioningServiceClient Create(string connectionString);
    Task<IndividualEnrollment> CreateOrUpdateIndividualEnrollmentAsync(ProvisioningServiceClient provisioningServiceClient, IndividualEnrollment enrollment);
    Task DeleteIndividualEnrollmentAsync(ProvisioningServiceClient provisioningServiceClient, string enrollmentId);

}