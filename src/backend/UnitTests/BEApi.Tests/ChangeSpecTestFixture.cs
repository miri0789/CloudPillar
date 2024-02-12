using System.Text;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Services;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Entities.Messages;
using Shared.Entities.Twin;

namespace Backend.BEApi.Tests;
public class ChangeSpecTestFixture
{
    private Mock<ITwinDiseredService> _twinDiseredServiceMock;
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private Mock<ICommonEnvironmentsWrapper> _environmentsWrapperMock;
    private DownloadSettings mockDownloadSettingsValue = new DownloadSettings();
    private Mock<IOptions<DownloadSettings>> mockDownloadSettings;
    private Mock<IRegistryManagerWrapper> _registryManagerWrapper;
    private IChangeSpecService _target;

    TwinDesired twinDesired = new TwinDesired()
    {
        ChangeSpec = new Dictionary<string, TwinChangeSpec>()
            {
                { CHANGE_SIGN_KEY, new TwinChangeSpec()
                {
                    Patch = new Dictionary<string, TwinAction[]>()
                    {
                        { "propName", new TwinAction[1]{
                            new DownloadAction() { Sign = "sign" }
                        } }
                    }
                } }
            }
    };
    private const string DEVICE_ID = "deviceId";
    private const string CHANGE_SIGN_KEY = "changeSpecSign";
    private const string SIGN = "ASZftuTGnLeppB4VYDU76cEuzAvrTnIdFLvfqcjLEnmLUE7mTSLhlWP1chQMZjm+s1gY85sNx6QZml3N+tpbnglrALwJ0mZlCTmZgdWiVsKi7Y1TD4HcmVeoc2L66uEyvLScGhIG0iblwvYJFC/hSQraKAb9hafN1U3PqI9CaohAMdMR";

    [SetUp]
    public void Setup()
    {
        _twinDiseredServiceMock = new Mock<ITwinDiseredService>();
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _environmentsWrapperMock = new Mock<ICommonEnvironmentsWrapper>();
        mockDownloadSettingsValue = DownloadSettingsHelper.SetDownloadSettingsValueMock();
        mockDownloadSettings = new Mock<IOptions<DownloadSettings>>();
        _registryManagerWrapper = new Mock<IRegistryManagerWrapper>();
        mockDownloadSettings.Setup(x => x.Value).Returns(mockDownloadSettingsValue);
        _httpRequestorServiceMock.Setup(x => x.SendRequest<byte[]>(It.Is<string>(x => x.Contains("Signing/SignData")), HttpMethod.Post, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(SIGN));
        _twinDiseredServiceMock.Setup(x => x.GetTwinDesiredAsync(DEVICE_ID)).ReturnsAsync(twinDesired);

        _target = new ChangeSpecService(_twinDiseredServiceMock.Object, _httpRequestorServiceMock.Object,
         _environmentsWrapperMock.Object, mockDownloadSettings.Object, _registryManagerWrapper.Object);
    }

    [Test]
    public async Task CreateChangeSpecKeySignatureAsync_ValidParameters_SendToSignData()
    {
        _twinDiseredServiceMock.Setup(x => x.GetTwinDesiredAsync(DEVICE_ID)).ReturnsAsync(new TwinDesired());
        await _target.CreateChangeSpecKeySignatureAsync(DEVICE_ID, CHANGE_SIGN_KEY);
        _twinDiseredServiceMock.Verify(x => x.SignTwinDesiredAsync(It.IsAny<TwinDesired>(), DEVICE_ID, CHANGE_SIGN_KEY, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task CreateFileKeySignatureAsync_ValidParameters_CallTwinDiseredService()
    {
        await _target.CreateFileKeySignatureAsync(DEVICE_ID, new SignFileEvent() { ChangeSpecKey = CHANGE_SIGN_KEY, PropName = "propName" });
        _httpRequestorServiceMock.Verify(x => x.SendRequest<byte[]>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task CreateFileKeySignatureAsync_SingatureIsEmpty_ThroeException()
    {

        _httpRequestorServiceMock.Setup(x => x.SendRequest<byte[]>(It.Is<string>(x => x.Contains("Signing/SignData")), HttpMethod.Post, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Encoding.UTF8.GetBytes(string.Empty));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
         _target.CreateFileKeySignatureAsync(DEVICE_ID, new SignFileEvent() { ChangeSpecKey = CHANGE_SIGN_KEY, PropName = "propName" }), "Signature is empty");
    }
}