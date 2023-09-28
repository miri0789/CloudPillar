using Microsoft.Azure.Devices.Provisioning.Service;

namespace Backend.Keyholder.Wrappers.Interfaces;
public interface IIndividualEnrollmentWrapper
{
    IndividualEnrollment Create(string deviceId, Attestation attestation);
}