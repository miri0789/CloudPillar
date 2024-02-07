using Backend.BEApi.Services;
using Backend.BEApi.Services.interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Moq;
using Shared.Entities.Twin;

namespace Backend.BEApi.Tests;
public class ChangeSpecTestFixture
{
    private Mock<ITwinDiseredService> _twinDiseredServiceMock;
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private Mock<IEnvironmentsWrapper> _environmentsWrapperMock;
    private IChangeSpecService _target;
    private const string DEVICE_ID = "deviceId";
    private const string CHANGE_SPEC_KEY = "changeSpec";
    private const string CHANGE_SIGN_KEY = "changeSpecSign";
    private const string SIGN = "ASZftuTGnLeppB4VYDU76cEuzAvrTnIdFLvfqcjLEnmLUE7mTSLhlWP1chQMZjm+s1gY85sNx6QZml3N+tpbnglrALwJ0mZlCTmZgdWiVsKi7Y1TD4HcmVeoc2L66uEyvLScGhIG0iblwvYJFC/hSQraKAb9hafN1U3PqI9CaohAMdMR";
    [SetUp]
    public void Setup()
    {
        _twinDiseredServiceMock = new Mock<ITwinDiseredService>();
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _target = new ChangeSpecService(_twinDiseredServiceMock.Object, _httpRequestorServiceMock.Object, _environmentsWrapperMock.Object);
    }
    [Test]
    public async Task AssignChangeSpecAsync_ValidParameters_CallTwinDiseredService()
    {
        await _target.AssignChangeSpecAsync(new object(), DEVICE_ID, CHANGE_SPEC_KEY);
        _twinDiseredServiceMock.Verify(x => x.AddChangeSpec(DEVICE_ID, CHANGE_SPEC_KEY, It.IsAny<object>()), Times.Once);
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
        _twinDiseredServiceMock.Setup(x => x.GetTwinDesiredAsync(DEVICE_ID)).ReturnsAsync(twinDesired);
        await _target.CreateFileKeySignatureAsync(DEVICE_ID, "propName", 0, CHANGE_SIGN_KEY, SIGN);
        _httpRequestorServiceMock.Verify(x => x.SendRequest<byte[]>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}