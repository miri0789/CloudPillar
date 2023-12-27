using Microsoft.Azure.Devices.Provisioning.Service;

namespace Backend.BEApi.Wrappers.Interfaces;
public interface IIndividualEnrollmentWrapper
{
    IndividualEnrollment Create(string deviceId, Attestation attestation);
}