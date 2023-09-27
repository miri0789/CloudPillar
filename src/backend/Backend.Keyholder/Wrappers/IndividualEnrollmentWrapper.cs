using Microsoft.Azure.Devices.Provisioning.Service;

namespace Backend.Keyholder.Wrappers;
public class IndividualEnrollmentWrapper : IIndividualEnrollmentWrapper
{
    public IndividualEnrollment Create(string deviceId, Attestation attestation)
    {
        return new IndividualEnrollment(deviceId, attestation);
    }
}