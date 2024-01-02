namespace CloudPillar.Agent.Handlers;
public interface IServerIdentityHandler
{

    Task HandleKnownIdentitiesFromCertificates(CancellationToken cancellationToken);
    Task UpdateKnownIdentitiesByCertFiles(string[] certificatesFiles, bool isInit, CancellationToken cancellationToken);
}