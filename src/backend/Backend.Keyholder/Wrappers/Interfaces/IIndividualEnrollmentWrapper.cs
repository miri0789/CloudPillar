using Microsoft.Azure.Devices.Provisioning.Service;

public interface IIndividualEnrollmentWrapper
{
    IndividualEnrollment Create(string deviceId, Attestation attestation);
}