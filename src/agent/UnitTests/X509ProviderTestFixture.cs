using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Entities.Authentication;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class X509ProviderTestFixture
{
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private Mock<IOptions<AuthenticationSettings>> _authenticationSettingsMock;
    private IX509Provider _target;
    private const string DEVICE_ID = "UnitTest";
    private const string SECRET_KEY = "secert";
    private const int EXPIRED_DAYS = 60;
    private const string CERTIFICATE_PREFIX = "CP";
    private const string ENVITOMENT = "UnitTest";



    [SetUp]
    public void Setup()
    {
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _authenticationSettingsMock = new Mock<IOptions<AuthenticationSettings>>();
        _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { CertificatePrefix = CERTIFICATE_PREFIX, Environment = ENVITOMENT });

        CreateTarget();
    }

    [Test]
    public async Task GenerateCertificate_CertificatePrefix_CreateCretificate()
    {

        var subjectName = $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CERTIFICATE_PREFIX}-{ENVITOMENT}-{DEVICE_ID}";
        var certificate = _target.GenerateCertificate(DEVICE_ID, SECRET_KEY, EXPIRED_DAYS);

        Assert.AreEqual(certificate.Subject, subjectName);
    }

    [Test]
    public async Task GenerateCertificate_EnviromentValueNull_PrefixWithNoEnviroment()
    {
        _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { CertificatePrefix = CERTIFICATE_PREFIX, Environment = null });
        CreateTarget();

        var subjectName = $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CERTIFICATE_PREFIX}-{DEVICE_ID}";
        var certificate = _target.GenerateCertificate(DEVICE_ID, SECRET_KEY, EXPIRED_DAYS);

        Assert.AreEqual(certificate.Subject, subjectName);
    }

    [Test]
    public async Task GenerateCertificate_PrefixValueNull_DefaultPrefixFromConstants()
    {
        _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { CertificatePrefix = null, Environment = ENVITOMENT });
        CreateTarget();

        var subjectName = $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{ProvisioningConstants.CLOUD_PILLAR_SUBJECT}-{ENVITOMENT}-{DEVICE_ID}";
        var certificate = _target.GenerateCertificate(DEVICE_ID, SECRET_KEY, EXPIRED_DAYS);

        Assert.AreEqual(certificate.Subject, subjectName);
    }

    private void CreateTarget()
    {
        _target = new X509Provider(
            _x509CertificateWrapperMock.Object,
            _authenticationSettingsMock.Object);
    }
}