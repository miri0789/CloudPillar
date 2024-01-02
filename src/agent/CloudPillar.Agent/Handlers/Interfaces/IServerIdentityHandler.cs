namespace CloudPillar.Agent.Handlers;
public interface IServerIdentityHandler
{

    Task HandleKnownIdentitiesFromCertificatesAsync(CancellationToken cancellationToken);
    Task UpdateKnownIdentitiesByCertFilesAsync(string[] certificatesFiles, bool isInit, CancellationToken cancellationToken);
}